using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using TEKLauncher.SteamInterop.Network.CM.Messages;
using TEKLauncher.SteamInterop.Network.CM.Messages.Bodies;
using TEKLauncher.Utils;
using static System.Array;
using static System.BitConverter;
using static System.Threading.WaitHandle;
using static TEKLauncher.SteamInterop.Network.Logger;

namespace TEKLauncher.SteamInterop.Network.CM
{
    internal class TCPConnection
    {
        private string EndpointAddress;
        private CancellationTokenSource Cancellator;
        private EncryptionFilter Encryptor;
        private EncryptionState State;
        private NetworkStream Stream;
        private Socket Socket;
        private Thread ConnectionThread;
        internal ConnectedEventHandler Connected;
        internal DisconnectedEventHandler Disconnected;
        internal MessageReceivedEventHandler MessageReceived;
        private readonly object Lock = new object();
        private static readonly RSAParameters Parameters = new RSAParameters
        {
            Exponent = new byte[] { 0x11 },
            Modulus = new byte[]
            {
                0xDF, 0xEC, 0x1A, 0xD6, 0x2C, 0x10, 0x66, 0x2C, 0x17, 0x35, 0x3A, 0x14, 0xB0, 0x7C, 0x59, 0x11,
                0x7F, 0x9D, 0xD3, 0xD8, 0x2B, 0x7A, 0xE3, 0xE0, 0x15, 0xCD, 0x19, 0x1E, 0x46, 0xE8, 0x7B, 0x87,
                0x74, 0xA2, 0x18, 0x46, 0x31, 0xA9, 0x03, 0x14, 0x79, 0x82, 0x8E, 0xE9, 0x45, 0xA2, 0x49, 0x12,
                0xA9, 0x23, 0x68, 0x73, 0x89, 0xCF, 0x69, 0xA1, 0xB1, 0x61, 0x46, 0xBD, 0xC1, 0xBE, 0xBF, 0xD6,
                0x01, 0x1B, 0xD8, 0x81, 0xD4, 0xDC, 0x90, 0xFB, 0xFE, 0x4F, 0x52, 0x73, 0x66, 0xCB, 0x95, 0x70,
                0xD7, 0xC5, 0x8E, 0xBA, 0x1C, 0x7A, 0x33, 0x75, 0xA1, 0x62, 0x34, 0x46, 0xBB, 0x60, 0xB7, 0x80,
                0x68, 0xFA, 0x13, 0xA7, 0x7A, 0x8A, 0x37, 0x4B, 0x9E, 0xC6, 0xF4, 0x5D, 0x5F, 0x3A, 0x99, 0xF9,
                0x9E, 0xC4, 0x3A, 0xE9, 0x63, 0xA2, 0xBB, 0x88, 0x19, 0x28, 0xE0, 0xE7, 0x14, 0xC0, 0x42, 0x89
            }
        };
        internal delegate void ConnectedEventHandler();
        internal delegate void DisconnectedEventHandler(bool UserInitiated);
        internal delegate void MessageReceivedEventHandler(byte[] Data);
        private void FinishConnect(bool Success)
        {
            if (Cancellator?.IsCancellationRequested ?? true)
            {
                if (Success)
                    Shutdown();
                Release(true);
            }
            else if (Success)
            {
                try
                {
                    lock (Lock)
                    {
                        Stream = new NetworkStream(Socket, false);
                        ConnectionThread = new Thread(NetworkLoop);
                    }
                    ConnectionThread.Start();
                    State = EncryptionState.Connected;
                }
                catch { Release(false); }
            }
            else
                Release(false);
        }
        private void NetworkLoop()
        {
            while (!Cancellator.IsCancellationRequested)
            {
                try
                {
                    if (!Socket.Poll(100000, SelectMode.SelectRead))
                        continue;
                }
                catch (SocketException) { break; }
                byte[] Data;
                try
                {
                    byte[] Buffer = new byte[8];
                    Stream.Read(Buffer, 0, 8);
                    int PacketLength = ToInt32(Buffer, 0);
                    if (ToUInt32(Buffer, 4) != 0x31305456U)
                        break;
                    Data = new byte[PacketLength];
                    int Offset = 0;
                    do
                    {
                        int BytesRead = Stream.Read(Data, Offset, PacketLength);
                        if (BytesRead == 0)
                            break;
                        Offset += BytesRead;
                        PacketLength -= BytesRead;
                    } while (PacketLength > 0);
                    if (PacketLength != 0)
                        break;
                }
                catch (IOException) { break; }
                try
                {
                    if (State == EncryptionState.Encrypted)
                        MessageReceived(Encryptor.Decrypt(Data));
                    else
                    {
                        MessageType Type = MessageType.Invalid;
                        if (Data.Length > 3)
                            Type = (MessageType)(ToUInt32(Data, 0) & ~0x80000000U);
                        if (State == EncryptionState.Connected && Type == MessageType.ChannelEncrypt)
                        {
                            RawMessage<ChannelEncrypt> Message = new RawMessage<ChannelEncrypt>(Data);
                            if (Message.Payload.Length >= 16)
                            {
                                RawMessage<ChannelEncrypt> Response = new RawMessage<ChannelEncrypt>(MessageType.ChannelEncryptResponse);
                                byte[] Challenge = Message.Payload, EncryptedBlob, SessionKey = new byte[32];
                                using (RandomNumberGenerator RNG = RandomNumberGenerator.Create())
                                    RNG.GetBytes(SessionKey);
                                using (RSA RSA = RSA.Create())
                                {
                                    RSA.ImportParameters(Parameters);
                                    byte[] BlobToEncrypt = new byte[32 + Challenge.Length];
                                    Copy(SessionKey, BlobToEncrypt, 32);
                                    Copy(Challenge, 0, BlobToEncrypt, 32, Challenge.Length);
                                    EncryptedBlob = RSA.Encrypt(BlobToEncrypt, RSAEncryptionPadding.OaepSHA1);
                                }
                                byte[] CRCHash;
                                using (CRC32 CRC = new CRC32())
                                    CRCHash = CRC.ComputeHash(EncryptedBlob);
                                int Length = EncryptedBlob.Length;
                                Response.Payload = new byte[Length + 8];
                                Copy(EncryptedBlob, Response.Payload, Length);
                                Copy(CRCHash, 0, Response.Payload, Length, 4);
                                Encryptor = new EncryptionFilter(SessionKey);
                                State = EncryptionState.Challenged;
                                Send(Response.Serialize());
                            }
                            else
                                Disconnect(false);
                        }
                        else if (State == EncryptionState.Challenged && Type == MessageType.ChannelEncryptResult)
                        {
                            if (new RawMessage<ChannelEncryptResult>(Data).Body.Success)
                            {
                                State = EncryptionState.Encrypted;
                                Log($"Established encrypted TCP connection to {EndpointAddress}");
                                Connected?.Invoke();
                            }
                            else
                                Disconnect(false);
                        }
                        else
                            Disconnect(false);
                    }
                }
                catch { }
            }
            bool UserInitiated = Cancellator.IsCancellationRequested;
            if (UserInitiated)
                Shutdown();
            Release(UserInitiated);
        }
        private void Release(bool UserInitiated)
        {
            lock (Lock)
            {
                Cancellator?.Dispose();
                Stream?.Dispose();
                Socket?.Dispose();
            }
            State = EncryptionState.Disconnected;
            Disconnected?.Invoke(UserInitiated);
        }
        private void Shutdown()
        {
            try
            {
                if (Socket?.Connected ?? false)
                {
                    Socket.Shutdown(SocketShutdown.Both);
                    Socket.Disconnect(true);
                }
            }
            catch { }
        }
        internal void Connect(IPEndPoint Endpoint)
        {
            lock (Lock)
            {
                EndpointAddress = Endpoint.ToString();
                Cancellator = new CancellationTokenSource();
                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveTimeout = 7000,
                    SendTimeout = 7000
                };
                if (Cancellator.IsCancellationRequested)
                    Release(true);
                else
                {
                    IAsyncResult AsyncResult = Socket.BeginConnect(Endpoint, null, null);
                    if (WaitAny(new[] { AsyncResult.AsyncWaitHandle, Cancellator.Token.WaitHandle }, 7000) == 0)
                    {
                        try
                        {
                            Socket.EndConnect(AsyncResult);
                            FinishConnect(true);
                        }
                        catch { FinishConnect(false); }
                    }
                    else
                        FinishConnect(false);
                }
            }
        }
        internal void Disconnect(bool UserInitiated)
        {
            lock (Lock)
            {
                Cancellator?.Cancel();
                State = EncryptionState.Disconnected;
                Disconnected?.Invoke(UserInitiated);
            }
        }
        internal void Send(byte[] Data)
        {
            if (State == EncryptionState.Encrypted)
                Data = Encryptor.Encrypt(Data);
            lock (Lock)
            {
                try
                {
                    Stream.Write(GetBytes(Data.Length), 0, 4);
                    Stream.Write(GetBytes(0x31305456U), 0, 4);
                    Stream.Write(Data, 0, Data.Length);
                }
                catch (IOException) { }
            }
        }
        private enum EncryptionState
        {
            Disconnected,
            Connected,
            Challenged,
            Encrypted
        }
    }
}