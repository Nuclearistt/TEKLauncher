using System.Collections.Generic;
using System.Threading;
using TEKLauncher.SteamInterop.Network.CM;
using TEKLauncher.SteamInterop.Network.CM.Messages.Bodies;
using TEKLauncher.Utils;
using static System.Threading.WaitHandle;
using static TEKLauncher.SteamInterop.Network.Logger;

namespace TEKLauncher.SteamInterop.Network
{
    internal static class SteamClient
    {
        private static VDFStruct Depots;
        private static readonly object Lock = new object();
        private static readonly CMClient Client = new CMClient();
        internal static void Disconnect()
        {
            lock (Lock)
            {
                if (Client.IsLogged)
                    Client.LogOff();
                else if (Client.IsConnected)
                    Client.Disconnect();
            }
        }
        internal static bool Connect()
        {
            lock (Lock)
            {
                if (!Client.IsLogged)
                {
                    CancellationTokenSource Cancellator = new CancellationTokenSource();
                    Client.LoggedOn = () => Cancellator.Cancel();
                    if (Client.IsConnected)
                        Client.LogOn();
                    else
                        Client.Connect();
                    WaitAny(new[] { Cancellator.Token.WaitHandle }, 7000);
                }
                return Client.IsLogged;
            }
        }
        internal static ulong GetManifestIDForDepot(uint DepotID)
        {
            lock (Lock)
            {
                CancellationTokenSource Cancellator = new CancellationTokenSource();
                if (Depots is null)
                {
                    Client.AppInfoReceived = (VDFStruct AppInfo) =>
                    {
                        try { Depots = AppInfo["depots"]; }
                        catch { }
                        Cancellator.Cancel();
                    };
                    Client.RequestAppInfo();
                    WaitAny(new[] { Cancellator.Token.WaitHandle }, 5000);
                    if (Depots is null)
                    {
                        Client.Disconnect();
                        return 0UL;
                    }
                }
                try
                {
                    ulong ManifestID = ulong.Parse(Depots[DepotID.ToString()]["manifests"]["public"].Value);
                    Log($"Resolved latest manifest ID for depot {DepotID}: {ManifestID}");
                    return ManifestID;
                }
                catch { throw new ValidatorException("Failed to parse app info"); }
            }
        }
        internal static ulong GetManifestIDForMod(uint AppID, ulong ModID)
        {
            lock (Lock)
            {
                CancellationTokenSource Cancellator = new CancellationTokenSource();
                ulong ManifestID = 0UL;
                Client.ModInfoReceived = (ID) =>
                {
                    ManifestID = ID;
                    Cancellator.Cancel();
                };
                Client.RequestModInfo(AppID, ModID);
                WaitAny(new[] { Cancellator.Token.WaitHandle }, 5000);
                if (ManifestID == 0UL)
                    Client.Disconnect();
                return ManifestID;
            }
        }
        internal static List<ItemDetails> GetDetails(params ulong[] IDs)
        {
            lock (Lock)
            {
                CancellationTokenSource Cancellator = new CancellationTokenSource();
                List<ItemDetails> ModsDetails = null;
                Client.ModsDetailsReceived = (Details) =>
                {
                    ModsDetails = Details;
                    Cancellator.Cancel();
                };
                Client.RequestModsDetails(IDs);
                WaitAny(new[] { Cancellator.Token.WaitHandle }, 7000);
                if (ModsDetails is null)
                    Client.Disconnect();
                return ModsDetails;
            }
        }
        internal static List<ItemDetails> GetQuery(int Page, string Search, out int TotalCount)
        {
            lock (Lock)
            {
                CancellationTokenSource Cancellator = new CancellationTokenSource();
                int TotalMods = 0;
                List<ItemDetails> QueryDetails = null;
                Client.QueryReceived = (Details, Total) =>
                {
                    TotalMods = Total;
                    QueryDetails = Details;
                    Cancellator.Cancel();
                };
                Client.RequestQuery(Page, Search);
                WaitAny(new[] { Cancellator.Token.WaitHandle }, 5000);
                if (QueryDetails is null)
                    Client.Disconnect();
                TotalCount = TotalMods;
                return QueryDetails;
            }
        }
    }
}