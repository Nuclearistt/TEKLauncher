using System;
using System.Runtime.InteropServices;

namespace TEKLauncher.SteamInterop.Steamworks
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CallbackBaseVTable
    {
        [NonSerialized]
        [MarshalAs(UnmanagedType.FunctionPtr)]
        internal RunCallResultDelegate RunCallResult;
        [NonSerialized]
        [MarshalAs(UnmanagedType.FunctionPtr)]
        internal RunCallbackDelegate RunCallback;
        [NonSerialized]
        [MarshalAs(UnmanagedType.FunctionPtr)]
        internal GetCallbackSizeDelegate GetCallbackSize;
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        internal delegate void RunCallResultDelegate(IntPtr SteamUGC, IntPtr Parameter, [MarshalAs(UnmanagedType.I1)]bool IOFailure, ulong SteamAPICall);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        internal delegate void RunCallbackDelegate(IntPtr SteamUGC, IntPtr Parameter);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        internal delegate int GetCallbackSizeDelegate(IntPtr SteamUGC);
    }
}