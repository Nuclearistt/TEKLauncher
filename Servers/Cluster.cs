using System.Collections.Generic;
using System.Threading;
using TEKLauncher.ARK;
using TEKLauncher.Net;
using static System.Threading.ThreadPool;
using static TEKLauncher.Net.ARKdictedData;

namespace TEKLauncher.Servers
{
    internal class Cluster
    {
        internal bool IsPvE;
        internal int PlayersLimit;
        internal string Discord, Hoster, Name;
        internal Dictionary<string, string> Info;
        internal Dictionary<string, ModRecord[]> Mods;
        internal Server[] Servers;
        private void RefreshServers(object State)
        {
            foreach (Server Server in Servers)
                Server.Refresh();
        }
        private void RequestArkoudaQuery(object State) => new ArkoudaQuery().Request();
        internal void Refresh()
        {
            foreach (Server Server in Servers)
            {
                Server.IsLoaded = false;
                Server.PlayersOnline = 0;
            }
            WaitCallback RefreshMethod;
            switch (Name)
            {
                case "Arkouda": RefreshMethod = RequestArkoudaQuery; break;
                case "ARKdicted": RefreshMethod = LoadServers; break;
                case "RUSSIA#": RefreshMethod = RUSSIAData.LoadServers; break;
                default: RefreshMethod = RefreshServers; break;
            }
            QueueUserWorkItem(RefreshMethod);
        }
    }
}