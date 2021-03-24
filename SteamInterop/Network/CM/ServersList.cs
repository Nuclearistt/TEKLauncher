using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using static System.Net.IPAddress;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Net.Downloader;
using static TEKLauncher.SteamInterop.Network.Logger;
using static TEKLauncher.SteamInterop.Network.CM.CMClient;

namespace TEKLauncher.SteamInterop.Network.CM
{
    internal static class ServersList
    {
        private static Stack<IPEndPoint> Servers = new Stack<IPEndPoint>();
        private static readonly object ListLock = new object();
        internal static void Initialize()
        {
            lock (ListLock)
                if (Servers.Count == 0)
                {
                    Log("CM servers list empty, requesting list from web API");
                    byte[] CMList = TryDownloadData($"{SteamWebAPI}ISteamDirectory/GetCMList/v1?cellid={CellID}&format=xml");
                    if (CMList is null)
                    {
                        Log("Failed to fetch list from web API");
                        throw new ValidatorException(LocString(LocCode.FetchServersFailed));
                    }
                    else
                    {
                        XmlDocument Document = new XmlDocument();
                        using (MemoryStream Stream = new MemoryStream(CMList))
                            Document.Load(Stream);
                        XmlNodeList Addresses = Document.DocumentElement.ChildNodes[0].ChildNodes;
                        IPEndPoint[] Endpoints = new IPEndPoint[Addresses.Count];
                        for (int Iterator = 0; Iterator < Endpoints.Length; Iterator++)
                        {
                            string Address = Addresses[Iterator].InnerText;
                            int ColonIndex = Address.IndexOf(':');
                            Endpoints[Iterator] = new IPEndPoint(Parse(Address.Substring(0, ColonIndex)), int.Parse(Address.Substring(++ColonIndex)));
                        }
                        Servers = new Stack<IPEndPoint>(Endpoints);
                    }
                }
        }
        internal static IPEndPoint NextServer()
        {
            lock (ListLock)
                return Servers.Count == 0 ? null : Servers.Pop();
        }
    }
}