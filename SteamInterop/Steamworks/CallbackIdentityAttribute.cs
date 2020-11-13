using System;

namespace TEKLauncher.SteamInterop.Steamworks
{
	[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
	internal class CallbackIdentityAttribute : Attribute
	{
		internal CallbackIdentityAttribute(int Identity) => this.Identity = Identity;
		internal readonly int Identity;
	}
}