﻿using System.Collections.Generic;
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
            for (int Iterator = 3; Iterator < Clusters.Length; Iterator++)
                if (Iterator != 5)
                    Clusters[Iterator].Refresh();
        }
        private static readonly string KillBillsIPS = "3282446663";
        private static readonly IPAddress KillBillsIP = new IPAddress(long.Parse(KillBillsIPS));
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
                    [string.Empty] = string.Join("\n",
                        string.Format(LocString(LocCode.MaxDinoLvl), 300),
                        string.Format(LocString(LocCode.Taming), 5),
                        string.Format(LocString(LocCode.Experience), 2.5F),
                        string.Format(LocString(LocCode.Harvesting), 4),
                        string.Format(LocString(LocCode.Breeding), 8),
                        string.Format(LocString(LocCode.Stacks), 12))
                },
                Mods = new Dictionary<string, ModRecord[]>(),
                Servers = new Server[0]
            },
            new Cluster
            {
                IsPvE = false,
                PlayersLimit = 50,
                Discord = DiscordArkouda,
                Hoster = "Perseus",
                Name = "Arkouda",
                Info = new Dictionary<string, string>
                {
                    ["All servers"] = string.Join("\n",
                        string.Format(LocString(LocCode.MaxDinoLvl), 300),
                        string.Format(LocString(LocCode.Experience), 10),
                        string.Format(LocString(LocCode.Breeding), 37),
                        string.Format(LocString(LocCode.Stacks), 50)),
                    ["Beginner PvP"] = string.Join("\n",
                        string.Format(LocString(LocCode.Taming), 20),
                        string.Format(LocString(LocCode.Harvesting), 20)),
                    ["Advanced PvP"] = string.Join("\n",
                        string.Format(LocString(LocCode.Taming), 10),
                        string.Format(LocString(LocCode.Harvesting), 10))
                },
                Mods = new Dictionary<string, ModRecord[]>(),
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
                    [string.Empty] = string.Join("\n",
                        string.Format(LocString(LocCode.MaxDinoLvl), 180),
                        string.Format(LocString(LocCode.Taming), 5),
                        string.Format(LocString(LocCode.Experience), 3),
                        string.Format(LocString(LocCode.Harvesting), 3),
                        string.Format(LocString(LocCode.Breeding), 3))
                },
                Mods = new Dictionary<string, ModRecord[]>
                {
                    ["Main cluster"] = new[]
                    {
                        new ModRecord(2018014354UL, "Dino storage v2", "40.44MB"),
                        new ModRecord(2006380780UL, "Awesome Spyglass", "3.26MB"),
                        new ModRecord(2006374844UL, "Dino Tracker", "9.56MB"),
                        new ModRecord(2060802637UL, "Super Structures", "66.7MB"),
                        new ModRecord(2006356933UL, "Rare Sightings", "342.62MB"),
                        new ModRecord(2004919122UL, "CKFR", "657.89MB"),
                        new ModRecord(2006816645UL, "Death Helper", "4.24MB")
                    },
                    ["Hope servers"] = new[]
                    {
                        new ModRecord(2006901571UL, "Hope Map", "953.58MB"),
                        new ModRecord(2148393197UL, "Primal Fear", "2.77GB"),
                        new ModRecord(2018014354UL, "Dino Storage v2", "40.44MB"),
                        new ModRecord(2006380780UL, "Awesome Spyglass", "3.26MB"),
                        new ModRecord(2060802637UL, "Super Structures", "66.7MB"),
                        new ModRecord(2200902771UL, "Primal Fear Genesis Expansion", "98.37MB"),
                        new ModRecord(2006408866UL, "Simple Spawners", "50.53MB")
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
                    new Server(KillBillsIP, CrystalIsles, 27088)
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