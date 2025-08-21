using System.Runtime.InteropServices;

namespace TEKLauncher.Utils;

/// <summary>Bindings for tek-steamclient library.</summary>
static partial class TEKSteamClient
{
	public static readonly string DllPath = $@"{App.AppDataFolder}\libtek-steamclient-1.dll";
	public static LibCtx? Ctx = null;
	public static AppManager? AppMng = null;

	#region Native Functions
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_version")]
	public static partial nint GetVersion();
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_lib_init")]
	private static partial nint LibInit([MarshalAs(UnmanagedType.I1)] bool useFileCache, [MarshalAs(UnmanagedType.I1)] bool disableLwsLogs);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_lib_cleanup")]
	private static partial void LibCleanup(nint libCtx);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_err_get_msgs")]
	private static partial ErrorMessages GetErrorMsgs(in Error err);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_err_release_msgs")]
	private static partial void ReleaseMsgs(ref ErrorMessages errMsgs);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_s3c_fetch_manifest", StringMarshalling = StringMarshalling.Utf8)]
	private static partial Error S3CFetchManifest(nint libCtx, string url, int timeoutMs);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_dd_estimate_disk_space")]
	public static partial long DeltaEstimateDiskSpace(nint delta);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_am_create", StringMarshalling = StringMarshalling.Utf16)]
	private static partial nint AmCreate(nint libCtx, string dir, out Error err);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_am_destroy")]
	private static partial void AmDestroy(nint am);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_am_set_ws_dir", StringMarshalling = StringMarshalling.Utf16)]
	private static partial Error AmSetWorkshopDir(nint am, string wsDir);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_am_get_item_desc")]
	private static unsafe partial AmItemDesc* AmGetItemDesc(nint am, ItemId *itemId);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_am_item_descs_lock")]
	private static partial void AmItemDescsLock(nint am);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_am_item_descs_unlock")]
	private static partial void AmItemDescsUnlock(nint am);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_am_check_for_upds")]
	private static partial Error AmCheckForUpdates(nint am, int timeoutMs);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_am_run_job")]
	private static unsafe partial Error AmRunJob(nint am, in AmJobArgs args, out AmItemDesc *itemDesc);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_am_pause_job")]
	private static partial void AmPauseJob(ref AmItemDesc itemDesc);
	[LibraryImport("libtek-steamclient-1.dll", EntryPoint = "tek_sc_am_cancel_job")]
	private static partial Error AmCancelJob(nint am, ref AmItemDesc itemDesc);
	#endregion

	#region Native Types
	[StructLayout(LayoutKind.Sequential)]
	public struct Error
	{
		public int Type;
		public int Primary;
		public int Auxiliary;
		public int Extra;
		public nint Uri;
		public readonly bool Success => Primary == 0;
		public readonly string Message 
		{
			get
			{
				var msgs = GetErrorMsgs(in this);
				var msg = $"An error has occurred\nType: ({Type}) {Marshal.PtrToStringUTF8(msgs.TypeStr)}\nPrimary message: ({Primary}) {Marshal.PtrToStringUTF8(msgs.Primary)}";
				if (msgs.Auxiliary != 0)
					msg += $"\nAuxiliary message: ({Auxiliary}) {Marshal.PtrToStringUTF8(msgs.Auxiliary)}";
				if (msgs.Extra != 0)
					msg += $"\n{Marshal.PtrToStringUTF8(msgs.Extra)}";
				if (Uri != 0)
				{
					msg += $"\n{Marshal.PtrToStringUTF8(msgs.UriType)}: {Marshal.PtrToStringUTF8(Uri)}";
				}
				ReleaseMsgs(ref msgs);
				return msg;
			}
		}
	}
	[StructLayout(LayoutKind.Sequential)]
	private struct ErrorMessages
	{
		public int Type;
		public nint TypeStr;
		public nint Primary;
		public nint Auxiliary;
		public nint Extra;
		public nint UriType;
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct ItemId
	{
		public uint AppId;
		public uint DepotId;
		public ulong WorkshopItemId;
	}
	[Flags]
	public enum AmItemStatus
	{
		Job = 1 << 0,
		UpdAvailable = 1 << 1
	}
	public enum AmJobStage
	{
		FetchingData,
		DwManifest,
		DwPatch,
		Verifying,
		Downloading,
		Pathcing,
		Installing,
		Deleting,
		Finalizing
	}
	public enum AmJobState
	{
		Stopped,
		Running,
		PausePending
	}
	public enum AmPatchStatus
	{ 
		Unknown,
		Unused,
		Used
	}
	[Flags]
	public enum AmUpdType
	{
		State = 1 << 0,
		Stage = 1 << 1,
		Progress = 1 << 2,
		DeltaCreated = 1 << 3
	}
	[StructLayout (LayoutKind.Sequential)]
	public struct AmJobDesc
	{
		public volatile AmJobState State;
		public AmJobStage Stage;
		public long ProgressCurrent;
		public long ProgressTotal;
		public ulong SourceManifestId;
		public ulong TargetManifestId;
		public AmPatchStatus PatchStatus;
		public nint Delta;
	}
	[StructLayout (LayoutKind.Sequential)]
	public struct AmItemDesc
	{
		public unsafe AmItemDesc* Next;
		public ItemId Id;
		public AmItemStatus Status;
		public ulong CurrentManifestId;
		public ulong LatestManifestId;
		public AmJobDesc Job;
	}
	[StructLayout (LayoutKind.Sequential)]
	struct AmJobArgs
	{
		public unsafe ItemId* ItemId;
		public ulong ManifestId;
		public nint UpdHandler;
		public byte ForceVerify;
	}
	public delegate void AmJobUpdFunc(ref AmItemDesc desc, AmUpdType upd_mask);
	#endregion

	public class LibCtx : SafeHandle
	{
		public LibCtx() : base(0, true)
		{
			handle = LibInit(true, true);
			if (handle == 0)
				throw new Exception("Failed to initialize tek-steamclient library context");
		}
		public override bool IsInvalid => handle == 0;
		protected override bool ReleaseHandle()
		{
			LibCleanup(handle);
			return true;
		}

		public Error SyncS3Manifest(string url) => S3CFetchManifest(handle, url, 16000);
	}
	public class AppManager : SafeHandle
	{
		readonly Error _err;
		public AppManager(LibCtx ctx, string path) : base(0, true)
		{
			handle = AmCreate(ctx.DangerousGetHandle(), path, out _err);
		}
		public override bool IsInvalid => handle == 0;
		protected override bool ReleaseHandle()
		{
			AmDestroy(handle);
			return true;
		}

		public Error CreationError => _err;
		public Error SetWorkshopDir(string path) => AmSetWorkshopDir(handle, path);
		public void LockItemDescs() => AmItemDescsLock(handle);
		public void UnlockItemDescs() => AmItemDescsUnlock(handle);
		public unsafe AmItemDesc* GetItemDesc(ItemId *id) => AmGetItemDesc(handle, id);
		public Error CheckForUpdates(int timeoutMs) => AmCheckForUpdates(handle, timeoutMs);
		public unsafe Error RunJob(in ItemId itemId, ulong manifestId, bool forceVerify, AmJobUpdFunc? updHandler, out AmItemDesc* itemDesc)
		{
			fixed (ItemId* itemIdPtr = &itemId)
			{
				var args = new AmJobArgs
				{
					ItemId = itemIdPtr,
					ManifestId = manifestId,
					UpdHandler = updHandler is null ? 0 : Marshal.GetFunctionPointerForDelegate(updHandler),
					ForceVerify = (byte)(forceVerify ? 1 : 0)
				};
				return AmRunJob(handle, in args, out itemDesc);
			}
		}
		public static void PauseJob(ref AmItemDesc itemDesc) => AmPauseJob(ref itemDesc);
		public Error CancelJob(ref AmItemDesc itemDesc) => AmCancelJob(handle, ref itemDesc);
	}
	public class Exception(string message) : System.Exception(message) {}
}
