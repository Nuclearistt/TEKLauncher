using System;
using System.Runtime.InteropServices;
using static System.GC;
using static System.IntPtr;
using static System.Runtime.InteropServices.GCHandle;
using static System.Runtime.InteropServices.Marshal;
using static TEKLauncher.SteamInterop.Steamworks.SteamAPI;

namespace TEKLauncher.SteamInterop.Steamworks
{
	internal sealed class CallResult<T> : IDisposable
	{
		internal CallResult(ulong Handle, APIDispatchDelegate Callback)
		{
			VTable = new CallbackBaseVTable
			{
				RunCallback = OnRunCallback,
				RunCallResult = OnRunCallResult,
				GetCallbackSize = OnGetCallbackSize
			};
			VTablePointer = AllocHGlobal(SizeOf(typeof(CallbackBaseVTable)));
			StructureToPtr(VTable, VTablePointer, false);
			CallbackBase = new CallbackBase
			{
				VTablePointer = VTablePointer,
				CallbackFlags = 0,
				Callback = GetCallbackIdentity(typeof(T))
			};
			CallbackBasePointer = Alloc(CallbackBase, GCHandleType.Pinned);
			this.Callback = Callback;
			if ((this.Handle = Handle) != 0UL)
				RegisterCallResult(CallbackBasePointer.AddrOfPinnedObject(), Handle);
		}
		~CallResult() => Dispose();
		private bool IsDisposed;
		private CallbackBaseVTable VTable;
		private GCHandle CallbackBasePointer;
		internal ulong Handle;
		private readonly int Size = SizeOf(typeof(T));
		private readonly APIDispatchDelegate Callback;
		private readonly CallbackBase CallbackBase;
		private readonly IntPtr VTablePointer = Zero;
		internal bool IsActive => Handle != 0UL;
		internal delegate void APIDispatchDelegate(T Parameter, bool IOFailure);
		private void OnRunCallback(IntPtr SteamUGC, IntPtr Parameter)
		{
			Handle = 0UL;
			Callback((T)PtrToStructure(Parameter, typeof(T)), false);
		}
		private void OnRunCallResult(IntPtr SteamUGC, IntPtr Parameter, bool IOFailure, ulong SteamAPICall)
		{
			if (SteamAPICall == Handle)
			{
				Handle = 0UL;
				Callback((T)PtrToStructure(Parameter, typeof(T)), IOFailure);
			}
		}
		private void Cancel()
		{
			if (Handle != 0UL)
			{
				UnregisterCallResult(CallbackBasePointer.AddrOfPinnedObject(), Handle);
				Handle = 0UL;
			}
		}
		public void Dispose()
		{
			if (!IsDisposed)
			{
				SuppressFinalize(this);
				Cancel();
				if (VTablePointer.ToInt64() != 0L)
					FreeHGlobal(VTablePointer);
				if (CallbackBasePointer.IsAllocated)
					CallbackBasePointer.Free();
				IsDisposed = true;
			}
		}
		private int OnGetCallbackSize(IntPtr SteamUGC) => Size;
		private static int GetCallbackIdentity(Type CallbackStruct) => ((CallbackIdentityAttribute)CallbackStruct.GetCustomAttributes(typeof(CallbackIdentityAttribute), false)[0]).Identity;
	}
}