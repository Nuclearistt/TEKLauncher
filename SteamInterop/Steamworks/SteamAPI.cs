using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media;
using TEKLauncher.Controls;
using TEKLauncher.Data;
using TEKLauncher.Utils;
using static System.IntPtr;
using static System.Threading.Tasks.Task;
using static System.Windows.Application;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Utils.UtilFunctions;
using static TEKLauncher.Utils.WinAPI;

namespace TEKLauncher.SteamInterop.Steamworks
{
    internal class SteamAPI
    {
        private int CallbackCompleteFlag = 0;
        private IntPtr HModule = Zero, ISteamUGC, ISteamUser, SteamClient;
        private CallResult<SubscribeResult> SubscribeCallResult;
        private CallResult<UnsubscribeResult> UnsubscribeCallResult;
        private static readonly string ETAString = LocString(LocCode.DownloadingMod), NAString = LocString(LocCode.NA);
        internal bool IsLoaded => HModule.ToInt64() != 0L;
        internal string SteamID => GetSteamID(ISteamUser).ToString();
        internal delegate void SetStatusDelegate(string Text, SolidColorBrush Color);
        private void SubscribedHandler(SubscribeResult Result, bool IOFailure) => CallbackCompleteFlag = (Result.Result == 1 && !IOFailure) ? 1 : -1;
        private void UnloadLibrary()
        {
            WinAPI.UnloadLibrary(HModule);
            WinAPI.UnloadLibrary(HModule);
            HModule = Zero;
        }
        private void UnsubscribedHandler(UnsubscribeResult Result, bool IOFailure) => CallbackCompleteFlag = (Result.Result == 1 && !IOFailure) ? 1 : -1;
        private bool TrackDownloadProgress(object Args)
        {
            object[] ArgsArray = (object[])Args;
            ulong ID = (ulong)ArgsArray[0];
            ProgressBar ProgressBar = (ProgressBar)ArgsArray[1];
            SetStatusDelegate SetStatusMethod = (SetStatusDelegate)ArgsArray[2];
            void SetStatus(string Text, SolidColorBrush Color) => Current.Dispatcher.Invoke(() => SetStatusMethod(Text, Color));
            Progress Progress = ProgressBar.Progress;
            if (!InitiateDownload(ISteamUGC, ID, false) || !GetDownloadProgress(ISteamUGC, ID, out _, out long Total))
            {
                SetStatus(LocString(LocCode.FailedToInitiateDw), DarkRed);
                return false;
            }
            int TimeoutCounter = 7;
            while (Total == 0L && --TimeoutCounter != 0)
            {
                Delay(1000).Wait();
                GetDownloadProgress(ISteamUGC, ID, out _, out Total);
            }
            if (Total == 0L)
            {
                SetStatus(LocString(LocCode.ProgressTrackerTimeout), DarkRed);
                return false;
            }
            TimeoutCounter = 70;
            void ProgressUpdatedHandler() => SetStatus(string.Format(ETAString, Progress.ETA < 0L ? NAString : ConvertTime(Progress.ETA)), YellowBrush);
            Current.Dispatcher.Invoke(() => ProgressBar.ProgressUpdated += ProgressUpdatedHandler);
            Progress.Total = Total;
            Current.Dispatcher.Invoke(ProgressBar.SetDownloadMode);
            while (GetDownloadProgress(ISteamUGC, ID, out long Downloaded, out Total) && --TimeoutCounter != 0)
            {
                Delay(100).Wait();
                if (Downloaded == 0L && Total == 0L)
                    break;
                if (Downloaded != Progress.Current)
                    TimeoutCounter = 70;
                Progress.Increase(Downloaded - Progress.Current);
            }
            Current.Dispatcher.Invoke(() => ProgressBar.ProgressUpdated -= ProgressUpdatedHandler);
            if ((GetModState(ISteamUGC, ID) & 4U) == 0U && Total - Progress.Current > 10485760L)
            {
                SetStatus(LocString(TimeoutCounter == 0 ? LocCode.ProgressTrackerTimeout : LocCode.ProgresTrackerStopped), DarkRed);
                return false;
            }
            Progress.Increase(Progress.Total - Progress.Current);
            return true;
        }
        private bool SubscribeMod(object ModID)
        {
            try
            {
                while (CallbackCompleteFlag != 0)
                    Delay(100).Wait();
                SubscribeCallResult = new CallResult<SubscribeResult>(SubscribeMod(ISteamUGC, (ulong)ModID), SubscribedHandler);
                int TimeoutCounter = 0;
                while (CallbackCompleteFlag == 0)
                {
                    RunCallbacks();
                    Delay(100).Wait();
                    if (TimeoutCounter++ == 20)
                    {
                        SubscribeCallResult.Dispose();
                        return false;
                    }
                }
                SubscribeCallResult.Dispose();
                bool Result = CallbackCompleteFlag == 1;
                CallbackCompleteFlag = 0;
                return Result;
            }
            catch { return false; }
        }
        private bool UnsubscribeMod(object ModID)
        {
            try
            {
                while (CallbackCompleteFlag != 0)
                    Delay(100).Wait();
                UnsubscribeCallResult = new CallResult<UnsubscribeResult>(UnsubscribeMod(ISteamUGC, (ulong)ModID), UnsubscribedHandler);
                int TimePassed = 0;
                while (CallbackCompleteFlag == 0)
                {
                    RunCallbacks();
                    Delay(100).Wait();
                    if (TimePassed++ == 20)
                    {
                        UnsubscribeCallResult.Dispose();
                        return false;
                    }
                }
                UnsubscribeCallResult.Dispose();
                bool Result = CallbackCompleteFlag == 1;
                CallbackCompleteFlag = 0;
                return Result;
            }
            catch { return false; }
        }
        internal void Unload()
        {
            SubscribeCallResult?.Dispose();
            UnsubscribeCallResult?.Dispose();
            SteamClient = ISteamUser = ISteamUGC = Zero;
            Dispose();
            Shutdown();
            UnloadLibrary();
        }
        internal void Load()
        {
            if ((HModule = LoadLibrary($@"{AppDataFolder}\steam_api64.dll")).ToInt64() == 0L)
                return;
            try
            {
                if (!Initialize())
                    UnloadLibrary();
                int SteamPipe = GetSteamPipe(), SteamUser = GetSteamUser();
                if (SteamPipe == 0)
                    UnloadLibrary();
                using (StringHandle Handle = new StringHandle("SteamClient020"))
                    if ((SteamClient = CreateInterface(Handle)).ToInt64() == 0L)
                        UnloadLibrary();
                using (StringHandle Handle = new StringHandle("SteamUser021"))
                    if ((ISteamUser = GetSteamUGC(SteamClient, SteamUser, SteamPipe, Handle)).ToInt64() == 0L)
                    {
                        SteamClient = Zero;
                        UnloadLibrary();
                    }
                using (StringHandle Handle = new StringHandle("STEAMUGC_INTERFACE_VERSION015"))
                    if ((ISteamUGC = GetSteamUGC(SteamClient, SteamUser, SteamPipe, Handle)).ToInt64() == 0L)
                    {
                        SteamClient = ISteamUser = Zero;
                        UnloadLibrary();
                    }
            }
            catch { UnloadLibrary(); }
        }
        internal Task<bool> TrackDownloadProgressAsync(ulong ModID, ProgressBar ProgressBar, SetStatusDelegate SetStatus) => Factory.StartNew(TrackDownloadProgress, new object[] { ModID, ProgressBar, SetStatus });
        internal Task<bool> SubscribeModAsync(ulong ModID) => Factory.StartNew(SubscribeMod, ModID);
        internal Task<bool> UnsubscribeModAsync(ulong ModID) => Factory.StartNew(UnsubscribeMod, ModID);
        internal ulong[] GetSubscribedMods()
        {
            try
            {
                int ModsCount = GetSubscribedModsCount(ISteamUGC);
                if (ModsCount < 0)
                    return new ulong[0];
                ulong[] IDsArray = new ulong[ModsCount];
                GetSubscribedMods(ISteamUGC, IDsArray, ModsCount);
                return IDsArray;
            }
            catch { return new ulong[0]; }
        }
        #region Native API Imports
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ReleaseCurrentThreadMemory")]
        private static extern void Dispose();
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_RunCallbacks")]
        private static extern void RunCallbacks();
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_Shutdown")]
        private static extern void Shutdown();
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamUGC_GetItemDownloadInfo")]
        private static extern bool GetDownloadProgress(IntPtr ISteamUGC, ulong ModID, out long Downloaded, out long Total);
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_Init")]
        private static extern bool Initialize();
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamUGC_DownloadItem")]
        private static extern bool InitiateDownload(IntPtr ISteamUGC, ulong ModID, bool HighPriority);
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_GetHSteamPipe")]
        private static extern int GetSteamPipe();
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_GetHSteamUser")]
        private static extern int GetSteamUser();
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamUGC_GetSubscribedItems")]
        private static extern int GetSubscribedMods(IntPtr ISteamUGC, [In][Out]ulong[] IDsArray, int ModsCount);
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamUGC_GetNumSubscribedItems")]
        private static extern int GetSubscribedModsCount(IntPtr ISteamUGC);
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamUGC_GetItemState")]
        private static extern uint GetModState(IntPtr ISteamUGC, ulong ModID);
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamUser_GetSteamID")]
        private static extern ulong GetSteamID(IntPtr ISteamUser);
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamUGC_SubscribeItem")]
        private static extern ulong SubscribeMod(IntPtr ISteamUGC, ulong ModID);
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamUGC_UnsubscribeItem")]
        private static extern ulong UnsubscribeMod(IntPtr ISteamUGC, ulong ModID);
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamInternal_CreateInterface")]
        private static extern IntPtr CreateInterface(StringHandle Version);
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamClient_GetISteamUGC")]
        private static extern IntPtr GetSteamUGC(IntPtr SteamClient, int SteamUser, int SteamPipe, StringHandle PCHVersion);
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamClient_GetISteamUser")]
        private static extern IntPtr GetSteamUser(IntPtr SteamClient, int SteamUser, int SteamPipe, StringHandle PCHVersion);
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_RegisterCallResult")]
        internal static extern void RegisterCallResult(IntPtr Callback, ulong SteamAPICall);
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_UnregisterCallResult")]
        internal static extern void UnregisterCallResult(IntPtr Callback, ulong SteamAPICall);
        #endregion
    }
}