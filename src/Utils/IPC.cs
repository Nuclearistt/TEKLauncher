using System.IO.MemoryMappedFiles;
using System.Threading;
using TEKLauncher.Steam;

namespace TEKLauncher.Utils;

/// <summary>Inter-process communication manager.</summary>
static class IPC
{
    /// <summary>Mod download progress to communicate to ARK Shellcode.</summary>
    static ModDownloadProgress s_progress;
    /// <summary>Cancellation token source for stopping <see cref="s_loopThread"/>.</summary>
    static readonly CancellationTokenSource s_cts = new();
    /// <summary>Signals that there is data available to read in <see cref="s_share"/>.</summary>
    static EventWaitHandle? s_inputEvent;
    /// <summary>Signals communicating process that launcher finished processing data and wrote response to <see cref="s_share"/>.</summary>
    static EventWaitHandle? s_outputEvent;
    /// <summary>Shared memory section.</summary>
    static MemoryMappedFile? s_share;
    /// <summary>View accessor for <see cref="s_share"/>.</summary>
    static MemoryMappedViewAccessor? s_accessor;
    /// <summary>Thread running <see cref="IpcLoop"/>.</summary>
    static Thread? s_loopThread;
    /// <summary>Waits for incoming IPC messages and processes them in a loop.</summary>
    static void IpcLoop()
    {
        while (WaitHandle.WaitAny(new WaitHandle[] { s_inputEvent!, s_cts.Token.WaitHandle }) == 0)
            switch (s_accessor!.ReadUInt32(0)) //Opcode
            {
                case 0: //Shellcode failure
                    uint errorCode = s_accessor.ReadUInt32(4);
                    Application.Current.Dispatcher.Invoke(() => Messages.Show("Error", errorCode == 8 ? "Steam API initialization failed" : string.Format(LocManager.GetString(LocCode.InjectionFailed), errorCode)));
                    break;
                case 1: //Start mod download
                    s_progress.Complete = false;
                    new Thread(TaskProcedure).Start(s_accessor.ReadUInt64(8));
                    s_outputEvent!.Set();
                    break;
                case 2: //Get mod download progress
                    s_accessor.Write(0, s_progress.Current);
                    s_accessor.Write(8, s_progress.Total);
                    s_accessor.Write(16, s_progress.Complete);
                    s_outputEvent!.Set();
                    break;
            }
    }
    /// <summary>Downloads/updates specified mod and updates download progress for ARK Shellcode.</summary>
    /// <param name="obj">Thread parameter, for this method it's a <see cref="ulong"/>.</param>
    static void TaskProcedure(object? obj)
    {
        ulong modId = (ulong)obj!;
        try
        {
            var details = Steam.CM.Client.GetModDetails(modId);
            if (details.Length != 0)
            {
                long currentProgress = 0;
                bool watchProgress = false; //Non-binary progresses should not be reported
                Client.RunTasks(346110, Tasks.GetUpdateData | Tasks.ReserveDiskSpace | Tasks.Download | Tasks.Install | Tasks.FinishModInstall, new()
                {
                    PrepareProgress = (mode, total) =>
                    {
                        watchProgress = mode;
                        if (mode)
                        {
                            currentProgress = 0;
                            s_progress.Total = total;
                        }
                    },
                    UpdateProgress = increment =>
                    {
                        if (watchProgress)
                        {
                            currentProgress += increment;
                            s_progress.Current = currentProgress;
                        }
                    }
                }, default, in details[0]);
            }
        }
        catch { }
        s_progress.Complete = true;
    }
    /// <summary>Closes and releases all IPC objects.</summary>
    public static void Dispose()
    {
        s_cts.Cancel();
        s_loopThread!.Join();
        s_outputEvent!.Dispose();
        s_inputEvent!.Dispose();
        s_accessor!.Dispose();
        s_share!.Dispose();
    }
    /// <summary>Creates IPC objects and checks whether another instance of the launcher is already running.</summary>
    /// <returns><see langword="false"/> if another instance of the launcher is already running; otherwise, <see langword="true"/>.</returns>
    public static bool Initialize()
    {
        if (EventWaitHandle.TryOpenExisting("TEKLauncherInput", out var result))
        {
            result.Dispose();
            return false;
        }
        s_share = MemoryMappedFile.CreateNew("TEKLauncherShare", 0x1000);
        s_accessor = s_share.CreateViewAccessor();
        s_inputEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "TEKLauncherInput");
        s_outputEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "TEKLauncherOutput");
        s_loopThread = new Thread(IpcLoop);
        s_loopThread.Start();
        return true;
    }
    /// <summary>Mod download progress structure exepcted by ARK Shellcode.</summary>
    struct ModDownloadProgress
    {
        public long Current;
        public long Total;
        public bool Complete;
    };
}