using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using static System.Net.Dns;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Net.Downloader;
using static TEKLauncher.SteamInterop.Network.Logger;
using static TEKLauncher.SteamInterop.Network.CM.CMClient;

namespace TEKLauncher.SteamInterop.Network.CDN
{
    internal static class ServersList
    {
        private static Stack<ServerRecord> Servers = new Stack<ServerRecord>();
        private static readonly object ListLock = new object();
        internal static void Initialize(int ThreadsCount)
        {
            lock (ListLock)
                if (Servers.Count < ThreadsCount)
                {
                    Log($"CDN servers list has less than {ThreadsCount} elements, requesting list from web API");
                    byte[] CDNList = TryDownloadData($"{SteamWebAPI}IContentServerDirectoryService/GetServersForSteamPipe/v1?cellid={CellID}&format=xml");
                    if (CDNList is null)
                    {
                        Log("Failed to fetch list from web API");
                        throw new ValidatorException(LocString(LocCode.FetchServersFailed));
                    }
                    else
                    {
                        XmlDocument Document = new XmlDocument();
                        using (MemoryStream Stream = new MemoryStream(CDNList))
                            Document.Load(Stream);
                        XmlNodeList Messages = Document.DocumentElement.ChildNodes[0].ChildNodes;
                        ServerRecord[] Records = new ServerRecord[Messages.Count];
                        for (int Iterator = 0; Iterator < Records.Length; Iterator++)
                        {
                            XmlNodeList Nodes = Messages[Iterator].ChildNodes;
                            string Host = null;
                            foreach (XmlNode Node in Nodes)
                                if (Node.Name == "vhost")
                                {
                                    Host = Node.InnerText;
                                    break;
                                }
                            if (Host is null)
                            {
                                Log("Failed to find vhost entry");
                                throw new ValidatorException(LocString(LocCode.IPNotResolved));
                            }
                            Records[Iterator] = new ServerRecord { Load = double.TryParse(Nodes[4].InnerText, out double Result) ? Result : 0D, Host = Host };
                        }
                        Servers = new Stack<ServerRecord>(Records.OrderBy(Record => Record.Load));
                    }
                }
        }
        internal static string NextServer()
        {
            lock (ListLock)
                return Servers.Count == 0 ? default : Servers.Pop().Host;
        }
    }
}