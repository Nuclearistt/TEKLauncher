using System.Collections.Generic;
using System.Net;
using TEKLauncher.ARK;
using static TEKLauncher.ARK.MapCode;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Data.LocalizationManager;

namespace TEKLauncher.Servers
{
    internal static class ClustersManager
    {
        static ClustersManager()
        {
            Clusters[0].Refresh();
            Clusters[2].Refresh();
            Clusters[3].Refresh();
        }
        private static readonly string KillBillsIPS = "3282446663";
        private static readonly IPAddress KillBillsIP = new IPAddress(long.Parse(KillBillsIPS));
        internal static readonly Cluster[] Clusters = new[]
        {
            new Cluster
            {
                IsPvE = true,
                PlayersLimit = 70,
                Discord = DiscordArkouda,
                Hoster = "Perseus & XOCO",
                Name = "Arkouda (Project-X)",
                Info = new Dictionary<string, string>
                {
                    [string.Empty] = string.Join("\n",
                        string.Format(LocString(LocCode.MaxDinoLvl), 300),
                        string.Format(LocString(LocCode.Taming), 5),
                        string.Format(LocString(LocCode.Experience), 2.5F),
                        string.Format(LocString(LocCode.Harvesting), 4),
                        string.Format(LocString(LocCode.Breeding), 8),
                        string.Format(LocString(LocCode.Stacks), 12))
                },
                Mods = new Dictionary<string, ModRecord[]>()
                {
                    [string.Empty] = new ModRecord[]
                    {
                        new ModRecord(2618264931UL, "Caballus", "9.55GB"),
                        new ModRecord(2613201271UL, "ARK Omega", "481.00MB"),
                        new ModRecord(2051206652UL, "Super Structures", "130.91MB"),
                        new ModRecord(2224171622UL, "Dino Storage v2", "40.93MB"),
                        new ModRecord(2317699592UL, "Upgrade Station", "234.39MB"),
                        new ModRecord(2030645484UL, "Awesome SpyGlass!", "3.33MB"),
                        new ModRecord(2464778317UL, "Awesome Teleporters!", "23.49MB"),
                        new ModRecord(2137263324UL, "Dino Tracker", "3.96MB"),
                        new ModRecord(2331916402UL, "Chat Evolved", "203.93KB")
                    }
                },
                Servers = new Server[]
                {
                    new Server(IPAddress.Parse("95.217.84.23"), MapCode.Mod, 27090, "Project-X S03")
                }
            },
            new Cluster
            {
                IsPvE = true,
                PlayersLimit = 30,
                Discord = DiscordARKdicted,
                Hoster = "Dz & Pixie",
                Name = "ARKdicted",
                Info = new Dictionary<string, string>
                {
                    [string.Empty] = string.Join("\n",
                        string.Format(LocString(LocCode.MaxDinoLvl), 180),
                        string.Format(LocString(LocCode.Taming), 5),
                        string.Format(LocString(LocCode.Experience), 3),
                        string.Format(LocString(LocCode.Harvesting), 3),
                        string.Format(LocString(LocCode.Breeding), 3))
                },
                Mods = new Dictionary<string, ModRecord[]>(),
                Servers = new Server[0]
            },
            new Cluster
            {
                IsPvE = false,
                PlayersLimit = 150,
                Discord = DiscordKillBills,
                Hoster = "Traccer",
                Name = "Kill Bills",
                Info = new Dictionary<string, string>
                {
                    [string.Empty] = string.Join("\n",
                        string.Format(LocString(LocCode.MaxDinoLvl), 180),
                        string.Format(LocString(LocCode.Taming), 10),
                        string.Format(LocString(LocCode.Experience), 4),
                        string.Format(LocString(LocCode.Harvesting), 5),
                        string.Format(LocString(LocCode.Breeding), 5))
                },
                Mods = null,
                Servers = new[]
                {
                    new Server(KillBillsIP, TheIsland, 27036),
                    new Server(KillBillsIP, TheCenter, 27037),
                    new Server(KillBillsIP, ScorchedEarth, 27039),
                    new Server(KillBillsIP, Ragnarok, 27035),
                    new Server(KillBillsIP, Aberration, 27038),
                    new Server(KillBillsIP, Extinction, 27040),
                    new Server(KillBillsIP, Valguero, 27145),
                    new Server(KillBillsIP, Genesis, 27146),
                    new Server(KillBillsIP, CrystalIsles, 27147),
                    new Server(KillBillsIP, Genesis2, 27148)
                }
            },
            new Cluster
            {
                IsPvE = true,
                PlayersLimit = 127,
                Discord = DiscordKillBills,
                Hoster = "Traccer",
                Name = "CallMeBob",
                Info = new Dictionary<string, string>
                {
                    [string.Empty] = string.Join("\n",
                        string.Format(LocString(LocCode.MaxDinoLvl), 180),
                        string.Format(LocString(LocCode.Taming), 10),
                        string.Format(LocString(LocCode.Experience), 4),
                        string.Format(LocString(LocCode.Harvesting), 5),
                        string.Format(LocString(LocCode.Breeding), 5))
                },
                Mods = null,
                Servers = new[]
                {
                    new Server(KillBillsIP, TheIsland, 27080),
                    new Server(KillBillsIP, TheCenter, 27084),
                    new Server(KillBillsIP, ScorchedEarth, 27083),
                    new Server(KillBillsIP, Ragnarok, 27081),
                    new Server(KillBillsIP, Aberration, 27085),
                    new Server(KillBillsIP, Extinction, 27082),
                    new Server(KillBillsIP, Valguero, 27086),
                    new Server(KillBillsIP, Genesis, 27087),
                    new Server(KillBillsIP, CrystalIsles, 27088),
                    new Server(KillBillsIP, Genesis2, 27089)
                }
            },
            new Cluster
            {
                IsPvE = false,
                PlayersLimit = 70,
                Discord = DiscordRUSSIA,
                Hoster = "overmind",
                Name = "RUSSIA#",
                Info = new Dictionary<string, string>(),
                Mods = null,
                Servers = new Server[0]
            },
            new Cluster { Name = LocString(LocCode.YourServers), Servers = new Server[0] }
        };
    }
}