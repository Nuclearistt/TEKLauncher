using System.IO.Compression;
using System.Net.WebSockets;
using System.Threading;
using Google.Protobuf;
using Microsoft.Toolkit.HighPerformance;
using TEKLauncher.Steam.CM.Messages;
using TEKLauncher.Steam.CM.Messages.Bodies;

namespace TEKLauncher.Steam.CM;

/// <summary>Represents a connection provider that uses WebSocket protocol to connect to Steam CM servers.</summary>
static class WebSocketConnection
{
    /// <summary>The interval between sending heartbeat messages in milliseconds.</summary>
    static int s_heartbeatInterval;
    /// <summary>ID of the current session.</summary>
    static int? s_sessionId;
    /// <summary>Steam ID of the anonymous user.</summary>
    static ulong? s_steamId;
    /// <summary>Cancellation token source for aborting the connection.</summary>
    static CancellationTokenSource? s_cts;
    /// <summary>WebSocket client.</summary>
    static ClientWebSocket? s_socket;
    /// <summary>Dedicated connection thread.</summary>
    static Thread? s_thread;
    /// <summary>Timer responsible for sending heartbeat messages.</summary>
    static readonly Timer s_timer = new(delegate
    {
        if (IsLoggedOn)
        {
            var message = new Message<Heartbeat>(MessageType.Heartbeat);
            message.Body.SendReply = true;
            Send(message);
        }
    });
    /// <summary>Gets a value that indicates whether client is currently logged onto a Steam CM server.</summary>
    public static bool IsLoggedOn { get; private set; }
    /// <summary>Gets or sets the list of cached CM server URLs.</summary>
    public static Stack<Uri> ServerList { get; set; } = new();
    /// <summary>Processes all incoming data in a loop.</summary>
    static void ConnectionLoop()
    {
        try
        {
            byte[] buffer = new byte[131072];
            s_heartbeatInterval = 2500;
            while (!s_cts!.IsCancellationRequested)
            {
                var receiveTask = s_socket!.ReceiveAsync(buffer, s_cts.Token);
                if (!receiveTask.Wait(s_heartbeatInterval * 2 + 300, s_cts.Token))
                {
                    s_cts.Cancel();
                    if (s_socket.State == WebSocketState.Open)
                        s_socket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, null, default).Wait(5000);
                    break;
                }
                var result = receiveTask.Result;
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (s_socket.State != WebSocketState.Closed && s_socket.State != WebSocketState.Aborted)
                        s_socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, s_cts.Token).Wait(5000);
                    break;
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                    continue;
                MessageReceived?.Invoke(buffer, result.Count);
            }
        }
        catch (TaskCanceledException) { }
        catch
        {
            try
            {
                s_cts!.Cancel();
                if (s_socket!.State == WebSocketState.Open)
                    s_socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default).Wait(5000);
            }
            catch { }
        }
        s_socket!.Dispose();
        s_cts!.Dispose();
        s_timer.Change(Timeout.Infinite, Timeout.Infinite);
        IsLoggedOn = false;
    }
    /// <summary>Sends a message to CM server.</summary>
    /// <param name="message">Message to send.</param>
    /// <returns><see langword="true"/> if message has been sent successfully within timeout period of 5 seconds; otherwise, <see langword="false"/></returns>
    static bool Send(Message message)
    {
        if (s_sessionId.HasValue)
            message.Header.SessionId = s_sessionId.Value;
        if (s_steamId.HasValue)
            message.Header.SteamId = s_steamId.Value;
        try { return s_socket!.SendAsync(message.Serialize(), WebSocketMessageType.Binary, true, s_cts!.Token).Wait(5000); }
        catch { return false;}
    }
    /// <summary>Initiates connection to a Steam CM server.</summary>
    /// <exception cref="SteamException">An error occured when connecting to CM server.</exception>
    public static void Connect()
    {
        s_sessionId = null;
        s_steamId = null;
        Uri endpointUrl = null!;
        for (int i = 0; i < 5; i++)
        {
            if (i % 2 == 0)
            {
                if (ServerList.Count == 0)
                {
                    Client.RefreshServerList();
                    if (ServerList.Count == 0)
                        throw new SteamException(LocManager.GetString(LocCode.CMConnectionFailed));
                }
                endpointUrl = ServerList.Pop();
            }
            s_cts = new();
            s_socket = new();
            bool connected = false;
            try { connected = s_socket.ConnectAsync(endpointUrl, s_cts.Token).Wait(5000); }
            catch { }
            if (!connected)
            {
                s_cts.Cancel();
                s_socket.Dispose();
                continue;
            }
            if (s_socket.State != WebSocketState.Open)
            {
                s_socket.Dispose();
                continue;
            }
            s_thread = new Thread(ConnectionLoop);
            s_thread.Start();
            var osVersion = Environment.OSVersion.Version;
            var logOnMessage = new Message<LogOn>(MessageType.LogOn);
            logOnMessage.Body.ProtocolVersion = 65580;
            logOnMessage.Body.CellId = Client.CellId;
            logOnMessage.Body.ClientLanguage = "english";
            logOnMessage.Body.ClientOsType = osVersion.Major switch
            {
                6 => osVersion.Minor switch
                {
                    1 => 10,
                    2 => 13,
                    3 => 14,
                    _ => 0,
                },
                10 when osVersion.Build >= 22000 => 20,
                10 => 16,
                _ => 0
            };
            logOnMessage.Header.SessionId = 0;
            logOnMessage.Header.SteamId = 117093590311632896;
            var response = GetMessage<LogOnResponse>(logOnMessage, MessageType.LogOnResponse);
            if (response is null)
            {
                Disconnect();
                continue;
            }
            int result = response.Body.Result;
            if (result == 1)
            {
                IsLoggedOn = true;
                s_heartbeatInterval = response.Body.HeartbeatInterval * 1000;
                s_sessionId = response.Header.SessionId;
                s_steamId = response.Header.SteamId;
                s_timer.Change(0, s_heartbeatInterval);
                Client.CellId = response.Body.CellId;
                return;
            }
            else
                Disconnect();
        }
        throw new SteamException(LocManager.GetString(LocCode.CMConnectionFailed));
    }
    /// <summary>Initiates disconnection from Steam CM server.</summary>
    public static void Disconnect()
    {
        if (IsLoggedOn)
        {
            if (!Send(new Message<Empty>(MessageType.LogOff)))
            {
                s_cts!.Cancel();
                if (s_socket!.State == WebSocketState.Open)
                    s_socket.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, null, default);
            }
        }
        else if (s_socket?.State == WebSocketState.Open)
            s_socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, s_cts!.Token);
        s_thread?.Join();
    }
    /// <summary>Sends a message to CM server and returns its response if there is any.</summary>
    /// <typeparam name="T">Type of expected response message body.</typeparam>
    /// <param name="message">Message to send.</param>
    /// <param name="expectedResponseType">Type of expected response message.</param>
    /// <param name="expectedTargetJobId">Target Job ID of expected response message.</param>
    /// <returns>Response message or <see langword="null"/> if none was received during timeout period of 5 seconds.</returns>
    public static Message<T>? GetMessage<T>(Message message, MessageType expectedResponseType, ulong expectedTargetJobId = 0) where T : IMessage, new()
    {
        using var waitEvent = new ManualResetEvent(false);
        Message<T>? result = null;
        void messageReceivedHandler(byte[] data, int size)
        {
            var type = (MessageType)(BitConverter.ToUInt32(data) & 0x7FFFFFFF);
            if (type == MessageType.ServerUnavailable)
            {
                Disconnect();
                waitEvent.Set();
            }
            else if (type == MessageType.Multi)
            {
                var message = new Message<Multi>(data, size);
                var stream = message.Body.MessagesData.Memory.AsStream();
                if (message.Body.UncompressedSize > 0)
                {
                    byte[] uncompressedPayload = new byte[message.Body.UncompressedSize];
                    try
                    {
                        using (var decompressorStream = new GZipStream(stream, CompressionMode.Decompress))
                        {
                            int bytesRead;
                            int offset = 0;
                            do
                            {
                                bytesRead = decompressorStream.Read(uncompressedPayload, offset, uncompressedPayload.Length - offset);
                                offset += bytesRead;
                            }
                            while (bytesRead > 0);
                        }
                        stream.Dispose();
                        stream = new MemoryStream(uncompressedPayload);
                    }
                    catch { return; }
                }
                Span<byte> buffer = stackalloc byte[4];
                using (stream)
                    while (stream.Read(buffer) == 4)
                    {
                        byte[] messageData = new byte[BitConverter.ToInt32(buffer)];
                        stream.Read(messageData);
                        type = (MessageType)(BitConverter.ToUInt32(messageData) & 0x7FFFFFFF);
                        if (type == MessageType.ServerUnavailable)
                        {
                            Disconnect();
                            waitEvent.Set();
                            return;
                        }
                        else if (type == expectedResponseType)
                        {
                            var header = new MessageHeader();
                            using var protoStream = new CodedInputStream(messageData, 8, BitConverter.ToInt32(messageData, 4));
                            protoStream.ReadRawMessage(header);
                            if (expectedTargetJobId == 0 || header.TargetJobId == expectedTargetJobId)
                            {
                                result = new Message<T>(messageData, messageData.Length);
                                waitEvent.Set();
                                return;
                            }
                        }
                    }
            }
            else if (type == expectedResponseType)
            {
                var header = new MessageHeader();
                using var stream = new CodedInputStream(data, 8, BitConverter.ToInt32(data, 4));
                stream.ReadRawMessage(header);
                if (expectedTargetJobId == 0 || header.TargetJobId == expectedTargetJobId)
                {
                    result = new Message<T>(data, size);
                    waitEvent.Set();
                }
            }
        };
        MessageReceived += messageReceivedHandler;
        if (Send(message))
            waitEvent.WaitOne(5000);
        MessageReceived -= messageReceivedHandler;
        return result;
    }
    /// <summary>Occurs when the connection receives a new message; message data array and size of the message in bytes are passed as arguments.</summary>
    static event Action<byte[], int>? MessageReceived;
}