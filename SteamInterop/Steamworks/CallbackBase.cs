using System;
using System.Runtime.InteropServices;

namespace TEKLauncher.SteamInterop.Steamworks
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct CallbackBase
	{
		internal const byte CallbackFlagsRegistered = 1, CallbackFlagsGameServer = 2;
		internal IntPtr VTablePointer;
		internal byte CallbackFlags;
		internal int Callback;
	}
}