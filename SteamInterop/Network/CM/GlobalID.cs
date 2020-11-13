using System.Diagnostics;
using static System.Diagnostics.Process;

namespace TEKLauncher.SteamInterop.Network.CM
{
    internal static class GlobalID
    {
        static GlobalID()
        {
            using (Process CurrentProcess = GetCurrentProcess())
                Value = (Value & 0xFFFFFF00000FFFFFUL) | (((((ulong)CurrentProcess.StartTime.Ticks - 0x8C6BDABF8998000UL) / 10000000UL) & 0xFFFFFUL) << 20);
        }
        private static ulong Counter = 1UL, Value = 0x3FFFFFFFFFFFFUL;
        internal static ulong NextJobID() => Value = (Value & 0xFFFFFFFFFFF00000UL) | (Counter++ & 0xFFFFFUL);
    }
}