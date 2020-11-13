using System.IO;
using static System.DateTime;
using static TEKLauncher.App;

namespace TEKLauncher.SteamInterop.Network
{
    internal static class Logger
    {
        private static readonly object LoggerLock = new object();
        private static readonly StreamWriter Writer = new StreamWriter($@"{AppDataFolder}\SteamNetwork.log", false) { AutoFlush = true };
        internal static void Close() => Writer.Close();
        internal static void Log(string Entry)
        {
            try
            {
                lock (LoggerLock)
                    Writer.WriteLine($"[{UtcNow:HH:mm:ss.ffffff}]: {Entry}");
            }
            catch { }
        }
    }
}