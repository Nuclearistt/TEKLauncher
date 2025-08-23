using System.Threading;

namespace TEKLauncher.Utils;

/// <summary>Inter-process communication manager.</summary>
static class IPC
{
    static EventWaitHandle? s_inputEvent;
    /// <summary>Closes and releases all IPC objects.</summary>
    public static unsafe void Dispose()
    {
        s_inputEvent!.Dispose();
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
        s_inputEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "TEKLauncherInput");
        return true;
    }
}