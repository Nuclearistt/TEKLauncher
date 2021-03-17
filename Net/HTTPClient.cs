using System;
using System.Collections.Generic;
using TEKLauncher.SteamInterop.Network;
using static System.IntPtr;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.SteamInterop.Network.Logger;
using static TEKLauncher.Utils.WinAPI;

namespace TEKLauncher.Net
{
    internal static class HTTPClient
    {
        static HTTPClient()
        {
            if ((Session = OpenWinHTTPSession("TEKLauncher", 1U, null, null, 0x10000000U)).ToInt64() == 0L)
                Log($"WinHTTP session failed to open, error code: {GetLastError()}");
            else if (WinHTTPSetCallback(Session, CallbackObject = Callback, 0x17E0000, Zero).ToInt64() == -1L)
            {
                Log($"Failed to set WinHTTP status callback, error code: {GetLastError()}");
                CloseWinHTTPHandle(Session);
                Session = Zero;
            }
            else if (!SetWinHTTPTimeouts(Session, 7000, 7000, 7000, 7000))
            {
                Log($"Failed to set WinHTTP timeouts, error code: {GetLastError()}");
                CloseWinHTTPHandle(Session);
                Session = Zero;
            }
        }
        private static readonly object Lock = new object();
        private static readonly IntPtr Session;
        internal static readonly Dictionary<long, HTTPConnection> Connections = new Dictionary<long, HTTPConnection>();
        internal static readonly WinHTTPStatusCallback CallbackObject;
        private static void Callback(IntPtr Session, long Context, uint Status, ref WinHTTPAsyncResult StatusInfo, int StatusInfoLength) => Connections[Context].ProcessCallback(Status, StatusInfoLength, ref StatusInfo);
        internal static void CloseSession()
        {
            if (Session.ToInt64() != 0L)
                CloseWinHTTPHandle(Session);
        }
        internal static HTTPConnection CreateConnection(string Host)
        {
            if (Session.ToInt64() == 0L)
                throw new ValidatorException(LocString(LocCode.FailedToInitDownloader));
            IntPtr Handle = WinHTTPConnectToServer(Session, Host, 443, 0U);
            if (Handle.ToInt64() == 0L)
            {
                Log($"Failed to create connection to {Host}, error code: {GetLastError()}");
                throw new ValidatorException(LocString(LocCode.FailedToConnectToSteam));
            }
            long Index = 0L;
            HTTPConnection Connection;
            lock (Lock)
            {
                for (; Connections.ContainsKey(Index); Index++);
                Connection = new HTTPConnection(Index, Handle);
                Connections[Index] = Connection;
            }
            return Connection;
        }
    }
}