using System.Runtime.InteropServices;

namespace TEKLauncher.SteamInterop.Steamworks
{
	[CallbackIdentity(Callback)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	internal struct UnsubscribeResult
	{
		internal const int Callback = 1315;
		internal int Result;
		internal ulong ModID;
	}
}