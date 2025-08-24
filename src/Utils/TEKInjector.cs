using System.Runtime.InteropServices;

namespace TEKLauncher.Utils;

/// <summary>Bindings for tek-injector library.</summary>
static partial class TEKInjector
{
	[LibraryImport("libtek-injector.dll", EntryPoint = "tek_inj_run_game")]
	public static partial void RunGame(ref Args args);

	#region Native Types
	public enum LoadType
	{
		FilePath,
		Pipe
	}
	public enum Res
	{
		Ok,
		GetTokenInfo,
		OpenToken,
		DuplicateToken,
		SetTokenInfo,
		CreateProcess,
		MemAlloc,
		MemWrite,
		SecDesc,
		CreateMapping,
		MapView,
		CreateThread,
		ThreadWait,
		DllLoad,
		ResumeThread,
	}
	[Flags]
	public enum InjFlags
	{
		HighPrio = 1 << 0,
		RunAsAdmin = 1 << 1
	}
	[StructLayout (LayoutKind.Sequential)]
	public struct Args
	{
		public nint ExePath;
		public nint CurrentDir;
		public nint DllPath;
		public LoadType Type;
		public int Argc;
		public nint Argv;
		public InjFlags Flags;
		public uint DataSize;
		public nint Data;
		public Res Result;
		public uint Win32Error;
	}
	#endregion
}
