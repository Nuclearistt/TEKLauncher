﻿using System;
using System.Runtime.InteropServices;

namespace TEKLauncher.Utils
{
    internal static class WinAPI
    {
        [DllImport("advapi32.dll")]
        internal static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAll, ref TokenPrivileges NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateProcessWithTokenW")]
        internal static extern bool CreateProcessWithToken(IntPtr Token, int LogonFlags, IntPtr FilePath, string CommandLine, int CreationFlags, IntPtr Environment, IntPtr CurrentDirectory, ref StartupInfo StartupInfo, out ProcessInfo ProcessInfo);
        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        internal static extern bool DuplicateToken(IntPtr ExistingToken, int AccessMode, IntPtr TokenAttributes, int ImpersonationLevel, int TokenType, out IntPtr NewToken);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "LookupPrivilegeValueW")]
        internal static extern bool LookupPrivilegeValue(string SystemName, string Name, ref LUID LUID);
        [DllImport("advapi32.dll")]
        internal static extern bool OpenProcessToken(IntPtr ProcessHandle, int AccessMode, out IntPtr TokenHandle);
        [DllImport("kernel32.dll")]
        internal static extern bool CloseHandle(IntPtr Handle);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetDiskFreeSpaceExW")]
        internal static extern bool GetDiskFreeSpace(string DirectoryPath, out long UserFreeSpaceAvailable, IntPtr UserTotalSpaceAvailable, IntPtr DiskFreeSpace);
        [DllImport("kernel32.dll", EntryPoint = "GetFileSizeEx")]
        internal static extern bool GetFileSize(IntPtr FileHandle, out long Size);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "SetDllDirectoryW")]
        internal static extern bool SetDLLDirectory(string DirectoryPath);
        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        internal static extern bool UnloadLibrary(IntPtr HModule);
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryW")]
        internal static extern IntPtr LoadLibrary(string LibraryPath);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateFileW")]
        internal static extern IntPtr OpenFile(string Name, uint AccessMode, int ShareMode, IntPtr SecurityAttributes, int CreationDisposition, int FlagsAndAttributes, IntPtr TemplateFile);
        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(int AccessMode, bool InheritHandle, int ProcessID);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "ShellExecuteW")]
        internal static extern bool Execute(IntPtr WindowHandle, string Operation, string File, string Parameters, string Directory, int ShowWindow);
        [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
        internal static extern int GetWindowProcessID(IntPtr WindowHandle, out int ProcessID);
        [DllImport("user32.dll")]
        internal static extern IntPtr GetShellWindow();
        [DllImport("wininet.dll", EntryPoint = "InternetGetConnectedState")]
        internal static extern bool GetConnectionState(out int Flags, int Reserved = 0);
        internal struct LUID { internal int LowPart, HighPart; }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct LUIDWithAttributes
        {
            internal LUID LUID;
            internal int Attributes;
        }
        internal struct ProcessInfo
        {
            internal IntPtr ProcessHandle, ThreadHandle;
            internal int ProcessID, ThreadID;
        }
        internal struct StartupInfo
        {
            internal int StructSize;
            internal IntPtr Reserved, Desktop, Title;
            internal int X, Y, XSize, YSize, XCharsSize, YCharsSize, FillAttribute, Flags;
            internal short ShowWindow, Reserved2;
            internal IntPtr Reserved3, StdInput, StdOutput, StdError;
        }
        internal struct TokenPrivileges
        {
            internal int PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            internal LUIDWithAttributes[] Privileges;
        }
    }
}