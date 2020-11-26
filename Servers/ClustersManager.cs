using System.Collections.Generic;
using System.Net;
using static TEKLauncher.ARK.MapCode;
using static TEKLauncher.Data.Links;

namespace TEKLauncher.Servers
{
    internal static class ClustersManager
    {
        static ClustersManager()
        {
            for (int Iterator = 2; Iterator < Clusters.Length; Iterator++)
                Clusters[Iterator].Refresh();
        }
        private static readonly string KillBillsIPS = "2103670342", ARKRussiaIPS = "2048058203";
        private static readonly IPAddress KillBillsIP = new IPAddress(long.Parse(KillBillsIPS)), ARKRussiaIP = new IPAddress(long.Parse(ARKRussiaIPS));
        internal static readonly Cluster[] Clusters = new[]
        {
            new Cluster
            {
                IsPvE = true,
                PlayersLimit = 50,
                Discord = DiscordArkouda,
                Hoster = "Perseus",
                Name = "Arkouda",
                Info = new Dictionary<string, string>
                {
                    [string.Empty] = "Max wild dino lvl 300\n" +
                    "Taming 5x\n" +
                    "Experience 2.5x\n" +
                    "Harvesting 4x\n" +
                    "Breeding 8x\n" +
                    "Stacks 12x"
                },
                Mods = new Dictionary<string, Dictionary<ulong, string>>
                {
                    ["All servers"] = new Dictionary<ulong, string>
                    {
                        [2051206652UL] = "Super Structures",
                        [2137263324UL] = "Dino Tracker",
                        [2277939297UL] = "EMS",
                        [1971614269UL] = "Simple Spawners",
                        [1972281180UL] = "TCs Auto Rewards",
                        [2030645484UL] = "Awesome Spyglass",
                        [2033361079UL] = "Auto-Harvest Ankylo",
                        [2212442912UL] = "The Tombstone",
                        [2224171622UL] = "Dino Storage v2",
                        [2035976232UL] = "Dino Colourizer",
                        [2236485756UL] = "Crystal Isles Dino Addition",
                        [2287982693UL] = "Oz-Ark Engram Tweaker"
                    },
                    ["Primal Fear server specific"] = new Dictionary<ulong, string>
                    {
                        [2223989048UL] = "Primal Fear",
                        [2229211757UL] = "Primal Fear Boss Expansion",
                        [2229213447UL] = "Primal Fear Aberration Expansion",
                        [2229214139UL] = "Primal Fear Genesis Expansion"
                    },
                    ["RP specific"] = new Dictionary<ulong, string>
                    {
                        [2106116037UL] = "CKF Remastered",
                        [2106126563UL] = "Advanced Rafts",
                        [2106180142UL] = "eco Trees",
                        [2106211489UL] = "eco Garden",
                        [2106364639UL] = "eco RP Deco"
                    }
                },
                Servers = new Server[0]
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
                    [string.Empty] = "Max wild dino lvl 180\n" +
                    "Taming 5x\n" +
                    "Experience 3x\n" +
                    "Harvesting 3x\n" +
                    "Breeding 3x"
                },
                Mods = new Dictionary<string, Dictionary<ulong, string>>
                {
                    ["Main cluster"] = new Dictionary<ulong, string>
                    {
                        [2018014354UL] = "Dino storage v2",
                        [2006380780UL] = "Awesome Spyglass",
                        [2006374844UL] = "Dino Tracker",
                        [2060802637UL] = "Super Structures",
                        [2006356933UL] = "Rare Sightings",
                        [2004919122UL] = "CKFR",
                        [2006816645UL] = "Death Helper"
                    },
                    ["Hope servers"] = new Dictionary<ulong, string>
                    {
                        [2006901571UL] = "Hope Map",
                        [2148393197UL] = "Primal Fear",
                        [2018014354UL] = "Dino Storage v2",
                        [2006380780UL] = "Awesome Spyglass",
                        [2060802637UL] = "Super Structures",
                        [2200902771UL] = "Primal Fear Genesis Expansion",
                        [2006408866UL] = "Simple Spawners"
                    }
                },
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
                    [string.Empty] = "Max wild dino lvl 180\n" +
                    "Taming 10x\n" +
                    "Experience 4x\n" +
                    "Harvesting 5x\n" +
                    "Breeding 5x"
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
                    new Server(KillBillsIP, CrystalIsles, 27147)
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
                    [string.Empty] = "Max wild dino lvl 180\n" +
                    "Taming 10x\n" +
                    "Experience 4x\n" +
                    "Harvesting 5x\n" +
                    "Breeding 5x"
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
                    new Server(KillBillsIP, CrystalIsles, 27088)
                }
            },
            new Cluster
            {
                IsPvE = false,
                PlayersLimit = 70,
                Discord = DiscordARKRussia,
                Hoster = "overmind",
                Name = "ARK Russia",
                Info = new Dictionary<string, string>
                {
                    [string.Empty] = "Max wild dino lvl 240\n" +
                    "Taming 5x\n" +
                    "Experience 5x\n" +
                    "Harvesting 20x\n" +
                    "Stacks 10x\n" +
                    "Newbies protection"
                },
                Mods = null,
                Servers = new[]
                {
                    new Server(ARKRussiaIP, TheIsland, 47016, "#2 The Island 10 Man"),
                    new Server(ARKRussiaIP, TheCenter, 47021, "#7 The Center 10 Man"),
                    new Server(ARKRussiaIP, Ragnarok, 47015, "#1 Ragnarok 10 Man"),
                    new Server(ARKRussiaIP, Aberration, 47018, "#4 Aberration 10 Man"),
                    new Server(ARKRussiaIP, Extinction, 47017, "#3 Extinction 10 Man"),
                    new Server(ARKRussiaIP, Valguero, 47022, "#8 Valguero 10 Man"),
                    new Server(ARKRussiaIP, Genesis, 47020, "#6 Genesis 10 Man"),
                    new Server(ARKRussiaIP, CrystalIsles, 47019, "#5 Crystal Isles 10 Man"),
                    new Server(ARKRussiaIP, TheIsland, 47028, "#10 The Island Beginners"),
                    new Server(ARKRussiaIP, Ragnarok, 47027, "#9 Ragnarok Beginners"),
                    new Server(ARKRussiaIP, Aberration, 47030, "#12 Aberration Beginners"),
                    new Server(ARKRussiaIP, Extinction, 47029, "#11 Extinction Beginners"),
                    new Server(ARKRussiaIP, Extinction, 47033, "#15 Valguero Beginners"),
                    new Server(ARKRussiaIP, Genesis, 47032, "#14 Genesis Beginners"),
                    new Server(ARKRussiaIP, CrystalIsles, 47031, "#13 Crystal Isles Beginners")
                }
            },
            new Cluster { Name = "Your servers", Servers = new Server[0] }
        };
    }
}