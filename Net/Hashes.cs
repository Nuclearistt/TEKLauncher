using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TEKLauncher.ARK;
using static System.Array;
using static System.IO.File;
using static System.Threading.Tasks.Task;
using static TEKLauncher.App;
using static TEKLauncher.Net.Downloader;

namespace TEKLauncher.Net
{
    internal class Hashes
    {
        internal readonly Dictionary<MapCode, byte[]> Checksums = new Dictionary<MapCode, byte[]>();
        internal bool Request()
        {
            byte[] Query = TryDownloadData("http://95.217.84.23/files/Ark/Hashes.sha");
            if (Query is null)
                return false;
            try
            {
                for (int i = 0; i < Query.Length / 20; i++)
                {
                    byte[] Buffer = new byte[20];
                    Copy(Query, i * 20, Buffer, 0, 20);
                    Checksums.Add((MapCode)i, Buffer);
                }
                return true;
            }
            catch (Exception Failure)
            {
                WriteAllText($@"{AppDataFolder}\QueryFailure.txt", $"{Failure.Message}\n{Failure.StackTrace}");
                return false;
            }
        }
        internal Task<bool> RequestAsync() => Run(Request);
    }
}