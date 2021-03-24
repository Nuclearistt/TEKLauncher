using System.Collections.Generic;
using System.Threading;
using TEKLauncher.SteamInterop.Network.CM;
using TEKLauncher.SteamInterop.Network.CM.Messages.Bodies;
using TEKLauncher.Utils;
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
                    ManualResetEvent Event = new ManualResetEvent(false);
                    Client.LoggedOn = () => Event.Set();
                    if (Client.IsConnected)
                        Client.LogOn();
                    else
                        Client.Connect();
                    Event.WaitOne(7000);
                    Event.Close();
                }
                return Client.IsLogged;
            }
        }
        internal static ulong GetManifestIDForDepot(uint DepotID)
        {
            lock (Lock)
            {
                ManualResetEvent Event = new ManualResetEvent(false);
                if (Depots is null)
                {
                    Client.AppInfoReceived = (VDFStruct AppInfo) =>
                    {
                        try { Depots = AppInfo["depots"]; }
                        catch { }
                        Event.Set();
                    };
                    Client.RequestAppInfo();
                    Event.WaitOne(5000);
                    Event.Close();
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
                ManualResetEvent Event = new ManualResetEvent(false);
                ulong ManifestID = 0UL;
                Client.ModInfoReceived = (ID) =>
                {
                    ManifestID = ID;
                    Event.Set();
                };
                Client.RequestModInfo(AppID, ModID);
                Event.WaitOne(5000);
                Event.Close();
                if (ManifestID == 0UL)
                    Client.Disconnect();
                return ManifestID;
            }
        }
        internal static List<ItemDetails> GetDetails(params ulong[] IDs)
        {
            lock (Lock)
            {
                ManualResetEvent Event = new ManualResetEvent(false);
                List<ItemDetails> ModsDetails = null;
                Client.ModsDetailsReceived = (Details) =>
                {
                    ModsDetails = Details;
                    Event.Set();
                };
                Client.RequestModsDetails(IDs);
                Event.WaitOne(5000);
                Event.Close();
                if (ModsDetails is null)
                    Client.Disconnect();
                return ModsDetails;
            }
        }
        internal static List<ItemDetails> GetQuery(int Page, string Search, out int TotalCount)
        {
            lock (Lock)
            {
                ManualResetEvent Event = new ManualResetEvent(false);
                int TotalMods = 0;
                List<ItemDetails> QueryDetails = null;
                Client.QueryReceived = (Details, Total) =>
                {
                    TotalMods = Total;
                    QueryDetails = Details;
                    Event.Set();
                };
                Client.RequestQuery(Page, Search);
                Event.WaitOne(5000);
                Event.Close();
                if (QueryDetails is null)
                    Client.Disconnect();
                TotalCount = TotalMods;
                return QueryDetails;
            }
        }
    }
}