using System;
using Microsoft.Win32.SafeHandles;
using static System.Text.Encoding;
using static System.Runtime.InteropServices.Marshal;

namespace TEKLauncher.SteamInterop.Steamworks
{
    internal class StringHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal StringHandle(string String) : base(true)
        {
            byte[] Buffer = new byte[ASCII.GetByteCount(String) + 1];
            ASCII.GetBytes(String, 0, String.Length, Buffer, 0);
            IntPtr Pointer = AllocHGlobal(Buffer.Length);
            Copy(Buffer, 0, Pointer, Buffer.Length);
            SetHandle(Pointer);
        }
        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
                FreeHGlobal(handle);
            return true;
        }
    }
}