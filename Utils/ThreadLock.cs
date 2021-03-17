using System;
using static System.IntPtr;
using static TEKLauncher.Utils.WinAPI;

namespace TEKLauncher.Utils
{
    internal class ThreadLock
    {
        ~ThreadLock() => Close();
        private bool Closed;
        private readonly IntPtr Event = CreateEvent(Zero, true, false, null);
        internal void Close()
        {
            if (Closed)
                return;
            CloseHandle(Event);
            Closed = true;
        }
        internal void Lock()
        {
            WaitEvent(Event, 0xFFFFFFFFU);
            ResetEvent(Event);
        }
        internal void Lock(uint Timeout) => WaitEvent(Event, Timeout);
        internal void LockWithoutReset() => WaitEvent(Event, 0xFFFFFFFFU);
        internal void Unlock() => SetEvent(Event);
    }
}