using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace TEKLauncher.Utils;

/// <summary>Consolidates function imports from Windows APIs and util methods using them.</summary>
static class WinAPI
{
    #region advapi32.dll
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    static extern bool CreateProcessWithTokenW(IntPtr hToken, int dwLogonFlags, string? lpApplicationName, string? lpCommandLine, uint dwCreationFlags, IntPtr lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);
    [DllImport("advapi32.dll")]
    static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, IntPtr lpTokenAttributes, uint ImpersonationLevel, uint TokenType, out IntPtr phNewToken);
    [DllImport("advapi32.dll")]
    static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);
    #endregion
    #region kernel32.dll
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr hObject);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern bool CreateProcessW(string? lpApplicationName, string? lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, [MarshalAs(UnmanagedType.Bool)]bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern bool GetDiskFreeSpaceExW(string lpDirectoryName, out long lpFreeBytesAvailableToCaller, out long lpTotalNumberOfBytes, out long lpTotalNumberOfFreeBytes);
    [DllImport("kernel32.dll")]
    static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, ulong dwSize, uint dwFreeType);
    [DllImport("kernel32.dll")]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, ulong nSize, out ulong lpNumberOfBytesWritten);
    [DllImport("kernel32.dll")]
    static extern uint ResumeThread(IntPtr hThread);
    [DllImport("kernel32.dll")]
    static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
    [DllImport("kernel32.dll")]
    static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, ulong dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);
    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr GetModuleHandleW(string lpModuleName);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)]bool bInheritHandle, uint dwProcessId);
    [DllImport("kernel32.dll")]
    static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, ulong dwSize, uint flAllocationType, uint flProtect);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibraryExW(string lpLibFileName, IntPtr hFile, uint dwFlags);
    #endregion
    #region user32.dll
    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")]
    static extern long SetWindowLongPtrW(IntPtr hWnd, int nIndex, long dwNewLong);
    [DllImport("user32.dll")]
    static extern IntPtr GetShellWindow();
    #endregion
    /// <summary>Enables minimize/restore animations for windows with <see cref="WindowStyle.None"/>.</summary>
    /// <param name="window">The window to set styles for.</param>
    /// <param name="allowResizing">Specifies whether the window should be resizeable.</param>
    /// <remarks>This simple trick took more than a year to be figured out, you cannot find this anywhere on the internet.</remarks>
    public static void SetWindowStyles(Window window, bool allowResizing)
    {
        long styles = 0x10CA0000;
        if (allowResizing)
            styles |= 0x50000;
        SetWindowLongPtrW(new WindowInteropHelper(window).Handle, -16, styles);
    }
    /// <summary>Retrieves the amount of free space on specified disk.</summary>
    /// <param name="directory">A directory on the disk whose free space amount will be retrieved.</param>
    /// <returns>The amount of disk free space in bytes.</returns>
    public static long GetDiskFreeSpace(string directory) => GetDiskFreeSpaceExW(directory, out long freeSpace, out _, out _) ? freeSpace : 0;
    /// <summary>Starts game process with specified privileges and optionally injects TEKInjector.dll.</summary>
    /// <param name="arguments">Command-line arguments to pass to game process.</param>
    public static void RunGameProcess(string arguments)
    {
        uint creationFlags = Game.UseTEKInjector ? 0x04U : 0; //CREATE_SUSPENDED if injection is requested
        var startupInfo = new STARTUPINFOW { cb = Marshal.SizeOf<STARTUPINFOW>() };
        PROCESS_INFORMATION processInformation;
        bool success;
        if (Game.RunAsAdmin)
            success = CreateProcessW(Game.ExePath, arguments, IntPtr.Zero, IntPtr.Zero, false, creationFlags, IntPtr.Zero, null, in startupInfo, out processInformation);
        else
        {
            //Copy non-elevated user token from shell window
            var shellWindow = GetShellWindow();
            if (shellWindow == IntPtr.Zero)
                return;
            _ = GetWindowThreadProcessId(shellWindow, out uint shellWindowProcessId);
            var shellProcess = OpenProcess(0x400, false, shellWindowProcessId);
            if (shellProcess == IntPtr.Zero)
                return;
            if (!OpenProcessToken(shellProcess, 0x2, out var shellProcessToken))
            {
                CloseHandle(shellProcess);
                return;
            }
            if (!DuplicateTokenEx(shellProcessToken, 0x18B, IntPtr.Zero, 2, 1, out var primaryToken))
            {
                CloseHandle(shellProcessToken);
                CloseHandle(shellProcess);
                return;
            }
            success = CreateProcessWithTokenW(primaryToken, 0, Game.ExePath, arguments, creationFlags, IntPtr.Zero, null, in startupInfo, out processInformation);
            CloseHandle(primaryToken);
            CloseHandle(shellProcessToken);
            CloseHandle(shellProcess);
        }
        if (!success)
            return;
        if (Game.UseTEKInjector)
        {
            var memory = VirtualAllocEx(processInformation.hProcess, IntPtr.Zero, 32, 0x2000 | 0x1000, 0x04);
            if (memory != IntPtr.Zero)
            {
                var dllNameBuffer = Marshal.StringToHGlobalUni("TEKInjector.dll");
                if (WriteProcessMemory(processInformation.hProcess, memory, dllNameBuffer, 32, out _))
                {
                    var module = GetModuleHandleW("kernel32.dll");
                    if (module != IntPtr.Zero)
                    {
                        var loadLibraryAdrress = GetProcAddress(module, "LoadLibraryW");
                        if (loadLibraryAdrress != IntPtr.Zero)
                        {
                            var thread = CreateRemoteThread(processInformation.hProcess, IntPtr.Zero, 0, loadLibraryAdrress, memory, 0, out _);
                            if (thread != IntPtr.Zero)
                            {
                                _ = WaitForSingleObject(thread, 100);
                                CloseHandle(thread);
                            }
                        }
                    }
                }
                Marshal.FreeHGlobal(dllNameBuffer);
                VirtualFreeEx(processInformation.hProcess, memory, 0, 0x8000);
            }
            _ = ResumeThread(processInformation.hThread);
        }
        CloseHandle(processInformation.hThread);
        CloseHandle(processInformation.hProcess);
    }
    #region Structures
    struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public uint dwProcessId, dwThreadId;
    }
    struct STARTUPINFOW
    {
        public int cb;
        public IntPtr lpReserved, lpDesktop, lpTitle;
        public uint dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public ushort wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }
    #endregion
}