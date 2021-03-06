﻿using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Threading.Tasks.Task;

namespace TEKLauncher.ARK
{
    internal static class DLCManager
    {
        internal static readonly DLC[] DLCs = new[]
        {
            new DLC("The Center", 346114U, true, false),
            new DLC("Scorched Earth", 375351U, false, true),
            new DLC("Ragnarok", 375354U, true, false),
            new DLC("Aberration", 375357U, false, true),
            new DLC("Extinction", 473851U, false, false),
            new DLC("Valguero", 473854U, true, true),
            new DLC("Genesis Part 1 & 2", 473857U, false, false),
            new DLC("Crystal Isles", 1318685U, true, false)
        };
        private static void CheckForUpdates(object Parameter)
        {
            Dictionary<MapCode, byte[]> Checksums = (Dictionary<MapCode, byte[]>)Parameter;
            foreach (DLC DLC in DLCs)
                if (Checksums.TryGetValue(DLC.Code, out byte[] Checksum))
                {
                    if (DLC.Code == MapCode.Genesis)
                        DLC.CheckGenesisForUpdates(Checksum, Checksums.TryGetValue(MapCode.Genesis2, out byte[] Gen2Checksum) ? Gen2Checksum : null);
                    else
                        DLC.CheckForUpdates(Checksum);
                }
                else
                    DLC.SetStatus(DLC.IsInstalled ? Status.Installed : Status.NotInstalled);
        }
        internal static DLC GetDLC(MapCode Code) => Code == MapCode.Genesis2 ? DLCs[6] : DLCs[(int)--Code];
        internal static Task CheckForUpdatesAsync(Dictionary<MapCode, byte[]> Checksums) => Factory.StartNew(CheckForUpdates, Checksums);
    }
}