using System.Runtime.InteropServices;

namespace TEKLauncher.SteamInterop.Steamworks
{
	[CallbackIdentity(Callback)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	internal struct SubscribeResult
	{
		internal const int Callback = 1313;
		internal int Result;
		internal ulong ModID;
	}
}