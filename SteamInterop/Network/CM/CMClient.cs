using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TEKLauncher.SteamInterop.Network.CM.Messages;
using TEKLauncher.SteamInterop.Network.CM.Messages.Bodies;
using TEKLauncher.Utils;
using static System.BitConverter;
using static System.Math;
using static System.Threading.Interlocked;
using static TEKLauncher.SteamInterop.Network.Logger;
using static TEKLauncher.SteamInterop.Network.CM.GlobalID;
using static TEKLauncher.SteamInterop.Network.CM.ServersList;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.SteamInterop.Network.CM
{
    internal class CMClient
    {
        internal CMClient()
        {
            HeartbeatTimer = new Timer(Heartbeat, null, -1, -1);
            Log("Instance of CM client created");
        }
        private int ExpectedServiceMethod;
        private int? SessionID;
        private ulong? SteamID;
        private CancellationTokenSource Cancellator;
        private volatile TCPConnection Connection;
        internal bool IsConnected, IsLogged;
        internal AppInfoReceivedEventHandler AppInfoReceived;
        internal LoggedOnEventHandler LoggedOn;
        internal ModInfoReceivedEventHandler ModInfoReceived;
        internal ModsDetailsReceivedEventHandler ModsDetailsReceived;
        internal QueryReceivedEventHandler QueryReceived;
        private readonly object ConnectionLock = new object();
        private readonly Timer HeartbeatTimer;
        internal static uint CellID;
        internal delegate void AppInfoReceivedEventHandler(VDFStruct AppInfo);
        internal delegate void LoggedOnEventHandler();
        internal delegate void ModInfoReceivedEventHandler(ulong ManifestID);
        internal delegate void ModsDetailsReceivedEventHandler(List<ItemDetails> Details);
        internal delegate void QueryReceivedEventHandler(List<ItemDetails> Details, int Total);
        private void ConnectedHandler()
        {
            IsConnected = true;
            LogOn();
        }
        private void DisconnectedHandler(bool UserInitiated)
        {
            TCPConnection ReleasedConnection = Exchange(ref Connection, null);
            if (!(ReleasedConnection is null))
            {
                Log("Disconnected from CM server");
                IsLogged = IsConnected = false;
                SessionID = null;
                SteamID = null;
                ReleasedConnection.Connected = null;
                ReleasedConnection.Disconnected = null;
                ReleasedConnection.MessageReceived = null;
                HeartbeatTimer.Change(-1, -1);
            }
        }
        private void MessageReceivedHandler(byte[] Data) => ClientMessageReceivedHandler(Data);
        private bool ClientMessageReceivedHandler(byte[] Data, List<Action> Callbacks = null)
        {
            MessageType Type = GetMessageType(Data);
            if (Type == MessageType.Invalid)
            {
                Disconnect(false);
                return false;
            }
            switch (Type)
            {
                case MessageType.Multi:
                {
                    Message<Multi> Message = new Message<Multi>(Data);
                    byte[] Payload = Message.Body.MessageBody;
                    if ((Message.Body.UncompressedSize ?? 0) > 0)
                    {
                        byte[] DecompressedPayload;
                        int UncompressedSize = Message.Body.UncompressedSize.Value;
                        try
                        {
                            using (MemoryStream Stream = new MemoryStream(Payload))
                            using (GZipStream Decompressor = new GZipStream(Stream, CompressionMode.Decompress))
                                Decompressor.Read(DecompressedPayload = new byte[UncompressedSize], 0, UncompressedSize);
                            Payload = DecompressedPayload;
                        }
                        catch { break; }
                    }
                    List<Action> CallbacksList = new List<Action>();
                    using (MemoryStream Stream = new MemoryStream(Payload))
                    using (BinaryReader Reader = new BinaryReader(Stream))
                        while (Stream.Position != Stream.Length)
                        {
                            byte[] Buffer = new byte[4];
                            Stream.Read(Buffer, 0, 4);
                            int PacketSize = ToInt32(Buffer, 0);
                            Buffer = new byte[PacketSize];
                            Stream.Read(Buffer, 0, PacketSize);
                            if (!ClientMessageReceivedHandler(Buffer, CallbacksList))
                                break;
                        }
                    foreach (Action Callback in CallbacksList)
                        Callback();
                    break;
                }
                case MessageType.LogOnResponse:
                {
                    Message<LogOn> Message = new Message<LogOn>(Data);
                    int Result = Message.Body.Result;
                    Log($"Received log on response, result code: {Result}");
                    if (Result == 1)
                    {
                        IsLogged = true;
                        SessionID = Message.Header.SessionID;
                        SteamID = Message.Header.SteamID;
                        HeartbeatTimer.Change(0, Message.Body.HeartbeatDelay * 1000);
                        Log($"Log on succeeded, initiating heartbeat with {Message.Body.HeartbeatDelay} seconds delay");
                    }
                    if (Callbacks is null)
                        LoggedOn?.Invoke();
                    else
                        Callbacks.Add(() => LoggedOn?.Invoke());
                    break;
                }
                case MessageType.ServiceMethodResponse:
                {
                    byte[] SerializedMethod = new Message<ServiceMethod>(Data).Body.SerializedMethod;
                    switch (ExpectedServiceMethod)
                    {
                        case 0:
                        {
                            ItemInfo Info = new ItemInfo();
                            Info.Deserialize(SerializedMethod);
                            WorkshopItem Item = Info.Item;
                            ulong ManifestID = Item.ManifestID;
                            Log($"Received latest manifest ID for mod {Item.ItemID}: {ManifestID}");
                            if (Callbacks is null)
                                ModInfoReceived(ManifestID);
                            else
                                Callbacks.Add(() => ModInfoReceived(ManifestID));
                            break;
                        }
                        case 1:
                        {
                            GetDetails Details = new GetDetails();
                            Details.Deserialize(SerializedMethod);
                            Log("Received mods details");
                            if (Callbacks is null)
                                ModsDetailsReceived(Details.Details);
                            else
                                Callbacks.Add(() => ModsDetailsReceived(Details.Details));
                            break;
                        }
                        case 2:
                        {
                            QueryFiles Query = new QueryFiles();
                            Query.Deserialize(SerializedMethod);
                            Log($"Received {Query.Details.Count} queried items details");
                            if (Callbacks is null)
                                QueryReceived(Query.Details, Query.Total);
                            else
                                Callbacks.Add(() => QueryReceived(Query.Details, Query.Total));
                            break;
                        }
                    }
                    break;
                }
                case MessageType.ProductInfoResponse:
                {
                    Message<ProductInfo> Message = new Message<ProductInfo>(Data);
                    if (Message.Body.App is null)
                        break;
                    Log("Received product info for app 346110");
                    VDFStruct AppInfo;
                    using (MemoryStream Stream = new MemoryStream(Message.Body.App.Buffer))
                    using (StreamReader Reader = new StreamReader(Stream))
                        AppInfo = new VDFStruct(Reader);
                    if (Callbacks is null)
                        AppInfoReceived(AppInfo);
                    else
                        Callbacks.Add(() => AppInfoReceived(AppInfo));
                    break;
                }
            }
            return true;
        }
        private void Heartbeat(object State) => Send(new Message<Empty>(MessageType.Heartbeat));
        internal void Connect()
        {
            lock (ConnectionLock)
            {
                if (IsConnectionAvailable())
                {
                    Disconnect();
                    Log("Connection to CM server requested");
                    Cancellator = new CancellationTokenSource();
                    Initialize();
                    IPEndPoint Server = NextServer();
                    if (!(Cancellator.IsCancellationRequested || Server is null))
                    {
                        Log("Picked a CM server from list, connecting...");
                        (Connection = new TCPConnection()
                        {
                            Connected = ConnectedHandler,
                            Disconnected = DisconnectedHandler,
                            MessageReceived = MessageReceivedHandler
                        }).Connect(Server);
                    }
                }
            }
        }
        internal void Disconnect(bool UserInitiated = true)
        {
            lock (ConnectionLock)
            {
                HeartbeatTimer.Change(-1, -1);
                try
                {
                    Cancellator?.Cancel();
                    Cancellator?.Dispose();
                }
                catch { }
                Connection?.Disconnect(UserInitiated);
            }
        }
        internal void LogOff()
        {
            Send(new Message<Empty>(MessageType.LogOff));
            Log("Log off request sent");
        }
        internal void LogOn()
        {
            Message<LogOn> Message = new Message<LogOn>(MessageType.LogOn);
            Message.Header.SessionID = 0;
            Message.Header.SteamID = 117093590311632896UL;
            Send(Message);
            Log("Anonymous log on request sent");
        }
        internal void RequestAppInfo()
        {
            Message<ProductInfo> Message = new Message<ProductInfo>(MessageType.ProductInfo);
            Message.Header.SourceJobID = NextJobID();
            Send(Message);
            Log("Requested info for app 346110");
        }
        internal void RequestModInfo(uint AppID, ulong ModID)
        {
            ExpectedServiceMethod = 0;
            Message<ServiceMethod> Message = new Message<ServiceMethod>(MessageType.ServiceMethod);
            Message.Header.SourceJobID = NextJobID();
            Message.Body.MethodName = "PublishedFile.GetItemInfo#1";
            Message.Body.SerializedMethod = new ItemInfo { AppID = AppID, Item = new WorkshopItem { ItemID = ModID } }.Serialize();
            Send(Message);
            Log($"Requested manifest ID for mod {ModID}");
        }
        internal void RequestModsDetails(ulong[] IDs)
        {
            ExpectedServiceMethod = 1;
            Message<ServiceMethod> Message = new Message<ServiceMethod>(MessageType.ServiceMethod);
            Message.Header.SourceJobID = NextJobID();
            Message.Body.MethodName = "PublishedFile.GetDetails#1";
            Message.Body.SerializedMethod = new GetDetails { IDs = IDs }.Serialize();
            Send(Message);
            Log($"Requested mods details for {IDs.Length} items");
        }
        internal void RequestQuery(int Page, string Search)
        {
            ExpectedServiceMethod = 2;
            Message<ServiceMethod> Message = new Message<ServiceMethod>(MessageType.ServiceMethod);
            Message.Header.SourceJobID = NextJobID();
            Message.Body.MethodName = "PublishedFile.QueryFiles#1";
            Message.Body.SerializedMethod = new QueryFiles { Page = Page, Search = Search }.Serialize();
            Send(Message);
            Log($"Requested 20 workshop items at page {Page}");
        }
        internal void Send(Message Message)
        {
            if (SessionID.HasValue)
                Message.Header.SessionID = SessionID;
            if (SteamID.HasValue)
                Message.Header.SteamID = SteamID;
            try { Connection?.Send(Message.Serialize()); }
            catch (IOException) { }
            catch (SocketException) { }
        }
        internal static MessageType GetMessageType(byte[] Data) => Data.Length < 4 ? MessageType.Invalid : (MessageType)(ToUInt32(Data, 0) & 0x7FFFFFFFU);
    }
}