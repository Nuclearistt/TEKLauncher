using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace TEKLauncher.Utils;

/// <summary>Consolidates function imports from Windows APIs and util methods using them.</summary>
static class WinAPI
{
    /// <summary>Creates game process and injects ARK Shellcode payload into it.</summary>
    /// <param name="commandLine">Command-line string for the process.</param>
    public static void RunGameProcess(string commandLine)
    {
        string imagePath = string.Concat(@"\??\", Game.ExePath);
        RtlCreateProcessParametersEx(out var parameters, new UnicodeString(imagePath), null, new UnicodeString(Path.GetDirectoryName(Game.ExePath)!), new UnicodeString(commandLine), null, null, null, null, null, 1);
        var imagePathBuffer = Marshal.StringToHGlobalUni(imagePath);
        var attributeList = new ProcessAttributeList
        {
            TotalSize = 40,
            Attributes = new ProcessAttribute[2]
            {
                new() { Attribute = 0x20005, Size = (ulong)(imagePath.Length * 2), Value = imagePathBuffer, ReturnSize = IntPtr.Zero },
                new() { Attribute = 0x60002, Size = 8, Value = IntPtr.Zero, ReturnSize = IntPtr.Zero }
            }
        };
        if (!Game.RunAsAdmin)
        {
            NtOpenProcessToken(new IntPtr(-1L), 0x2, out var token);
            NtDuplicateToken(token, 0x81, IntPtr.Zero, false, 1, out attributeList.Attributes[1].Value);
            NtClose(token);
            var sid = Marshal.AllocHGlobal(12);
            Marshal.WriteInt32(sid, 0x101);
            Marshal.WriteInt32(sid, 4, 0x10000000);
            Marshal.WriteInt32(sid, 8, 0x2000);
            NtSetInformationToken(attributeList.Attributes[1].Value, 25, new TokenMandatoryLabel { Sid = sid, Attributes = 0x20 }, 16);
            Marshal.FreeHGlobal(sid);
            attributeList.TotalSize += 32;
        }
        NtCreateUserProcess(out var process, out var thread, 0x1FFFFF, 0x1FFFFF, IntPtr.Zero, IntPtr.Zero, 0, 0, parameters, new ProcessCreateInformation { Size = 88 }, attributeList);
        NtClose(thread);
        if (attributeList.Attributes[1].Value != IntPtr.Zero)
            NtClose(attributeList.Attributes[1].Value);
        Marshal.FreeHGlobal(imagePathBuffer);
        RtlDestroyProcessParameters(parameters);
        if (Game.HighProcessPriority)
            NtSetInformationProcess(process, 18, 0x300, 2);
        using var resourceStream = Application.GetResourceStream(new("pack://application:,,,/res/Payload.bin")).Stream;
        byte[] payload = new byte[resourceStream.Length];
        resourceStream.Read(payload);
        IntPtr module = GetModuleHandleW("ntdll.dll");
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x0, 8), GetProcAddress(module, "LdrGetDllHandleEx").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x8, 8), GetProcAddress(module, "LdrGetProcedureAddressForCaller").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x10, 8), GetProcAddress(module, "NtAllocateVirtualMemory").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x18, 8), GetProcAddress(module, "NtClose").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x20, 8), GetProcAddress(module, "NtDelayExecution").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x28, 8), GetProcAddress(module, "NtMapViewOfSection").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x30, 8), GetProcAddress(module, "NtOpenEvent").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x38, 8), GetProcAddress(module, "NtOpenFile").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x40, 8), GetProcAddress(module, "NtOpenSection").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x48, 8), GetProcAddress(module, "NtProtectVirtualMemory").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x50, 8), GetProcAddress(module, "NtQueryAttributesFile").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x58, 8), GetProcAddress(module, "NtQueryDirectoryFile").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x60, 8), GetProcAddress(module, "NtQueryVirtualMemory").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x68, 8), GetProcAddress(module, "NtSetEvent").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x70, 8), GetProcAddress(module, "NtUnmapViewOfSection").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x78, 8), GetProcAddress(module, "NtWaitForSingleObject").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x80, 8), GetProcAddress(module, "RtlUnicodeToUTF8N").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x88, 8), GetProcAddress(GetModuleHandleW("kernelbase.dll"), "BaseGetNamedObjectDirectory").ToInt64());
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x1F8, 8), Steam.App.CurrentUserStatus.SteamId64);
        IntPtr payloadRegionAddress = IntPtr.Zero;
        ulong payloadRegionSize = (ulong)payload.Length;
        NtAllocateVirtualMemory(process, ref payloadRegionAddress, 0, ref payloadRegionSize, 0x3000, 0x40);
        long addressAnchor = payloadRegionAddress.ToInt64();
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0xC0, 8), addressAnchor + 0x138);
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0xD0, 8), addressAnchor + 0x158);
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0xE0, 8), addressAnchor + 0x17A);
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0xF0, 8), addressAnchor + 0x19C);
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x100, 8), addressAnchor + 0x1C0);
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x110, 8), addressAnchor + 0x1CA);
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x120, 8), addressAnchor + 0x1E2);
        BitConverter.TryWriteBytes(new Span<byte>(payload, 0x130, 8), addressAnchor + 0x1EB);
        NtWriteVirtualMemory(process, payloadRegionAddress, payload, payload.Length, IntPtr.Zero);
        NtCreateThreadEx(out thread, 0x1FFFFF, IntPtr.Zero, process, addressAnchor + 0xB60, (ulong)Steam.App.CurrentUserStatus.GameStatus, 0, 0, 0, 0, IntPtr.Zero);
        NtClose(thread);
        NtClose(process);
    }
    #region ntdll.dll
    [DllImport("ntdll.dll")]
    static extern void NtAllocateVirtualMemory(IntPtr process, ref IntPtr baseAddress, ulong zeroBits, ref ulong regionSize, uint allocationType, uint protection);
    [DllImport("ntdll.dll")]
    static extern void NtClose(IntPtr handle);
    [DllImport("ntdll.dll")]
    static extern void NtCreateThreadEx(out IntPtr handle, uint accessMask, IntPtr attributes, IntPtr process, long startRoutine, ulong argument, uint flags, ulong zeroBits, ulong stackSize, ulong maxStackSize, IntPtr attributeList);
    [DllImport("ntdll.dll")]
    static extern void NtCreateUserProcess(out IntPtr process, out IntPtr thread, uint processAccessMask, uint threadAccessMask, IntPtr processAttributes, IntPtr threadAttributes, uint processFlags, uint threadFlags, IntPtr parameters, in ProcessCreateInformation createInfo, in ProcessAttributeList attributeList);
    [DllImport("ntdll.dll")]
    static extern void NtDuplicateToken(IntPtr token, uint accessMask, IntPtr attributes, bool effectiveOnly, uint type, out IntPtr newToken);
    [DllImport("ntdll.dll")]
    static extern void NtOpenProcessToken(IntPtr process, uint accessMask, out IntPtr handle);
    [DllImport("ntdll.dll")]
    static extern void NtSetInformationProcess(IntPtr process, uint informationClass, in ushort information, uint informationSize);
    [DllImport("ntdll.dll")]
    static extern void NtSetInformationToken(IntPtr token, uint informationClass, in TokenMandatoryLabel information, uint informationSize);
    [DllImport("ntdll.dll")]
    static extern void NtWriteVirtualMemory(IntPtr process, IntPtr address, byte[] buffer, int bufferSize, IntPtr bytesWritten);
    [DllImport("ntdll.dll", CharSet = CharSet.Unicode)]
    static extern void RtlCreateProcessParametersEx(out IntPtr processParameters, UnicodeString imagePath, UnicodeString? dllPath, UnicodeString? currentDirectory, UnicodeString? commandLine, string? environment, UnicodeString? windowTitle, UnicodeString? desktopInfo, UnicodeString? shellInfo, UnicodeString? runtimeData, uint flags);
    [DllImport("ntdll.dll")]
    static extern void RtlDestroyProcessParameters(IntPtr processParameters);
    #endregion
    #region kernel32.dll
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern bool GetDiskFreeSpaceExW(string lpDirectoryName, out long lpFreeBytesAvailableToCaller, out long lpTotalNumberOfBytes, out long lpTotalNumberOfFreeBytes);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr GetModuleHandleW(string moduleName);
    [DllImport("kernel32.dll")]
    static extern IntPtr GetProcAddress(IntPtr module, string procName);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibraryExW(string lpLibFileName, IntPtr hFile, uint dwFlags);
    #endregion
    #region user32.dll
    [DllImport("user32.dll")]
    static extern long SetWindowLongPtrW(IntPtr hWnd, int nIndex, long dwNewLong);
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
    /// <summary>Retrieves major version of currently installed DirectX.</summary>
    /// <returns>DirectX major version number.</returns>
    public static uint GetDirectXMajorVersion()
    {
        IDxDiagProvider? provider = null;
        IDxDiagContainer? rootContainer = null;
        IDxDiagContainer? systemInfoContainer = null;
        try
        {
            provider = (IDxDiagProvider)new DxDiagProvider();
            provider.Initialize(new()
            {
                Size = Marshal.SizeOf<DxDiagInitParams>(),
                HeaderVersion = 111,
                AllowWHQLChecks = false,
                Reserved = IntPtr.Zero
            });
            provider.GetRootContainer(out rootContainer);
            rootContainer.GetChildContainer("DxDiag_SystemInfo", out systemInfoContainer);
            systemInfoContainer.GetProp("dwDirectXVersionMajor", out object variant);
            return (uint)variant;
        }
        finally
        {
            if (systemInfoContainer is not null)
                Marshal.ReleaseComObject(systemInfoContainer);
            if (rootContainer is not null)
                Marshal.ReleaseComObject(rootContainer);
            if (provider is not null)
                Marshal.ReleaseComObject(provider);
        }
    }
    /// <summary>Retrieves the amount of free space on specified disk.</summary>
    /// <param name="directory">A directory on the disk whose free space amount will be retrieved.</param>
    /// <returns>The amount of disk free space in bytes.</returns>
    public static long GetDiskFreeSpace(string directory) => GetDiskFreeSpaceExW(directory, out long freeSpace, out _, out _) ? freeSpace : 0;
    struct DxDiagInitParams
    {
        public int Size;
        public uint HeaderVersion;
        public bool AllowWHQLChecks;
        public IntPtr Reserved;
    };
    struct ProcessAttribute
    {
        public ulong Attribute;
        public ulong Size;
        public IntPtr Value;
        public IntPtr ReturnSize;
    };
    struct ProcessAttributeList
    {
        public ulong TotalSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public ProcessAttribute[] Attributes;
    };
    struct ProcessCreateInformation
    {
        public ulong Size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public ulong[] Unused;
    };
    struct TokenMandatoryLabel
    {
        public IntPtr Sid;
        public ulong Attributes;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    class UnicodeString
    {
        public ushort Size;
        public ushort MaxSize;
        public string Buffer;
        public UnicodeString(string str)
        {
            MaxSize = Size = (ushort)(str.Length * 2);
            Buffer = str;
        }
    }
    [ComImport]
    [Guid("A65B8071-3BFE-4213-9A5B-491DA4461CA7")]
    class DxDiagProvider { }
    [Guid("7D0F462F-4064-4862-BC7F-933E5058C10F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDxDiagContainer
    {
        void EnumChildContainerNames(uint index, string container, uint containerLength);
        void EnumPropNames(uint index, string name, uint nameLength);
        void GetChildContainer(string container, out IDxDiagContainer instance);
        void GetNumberOfChildContainers(out uint count);
        void GetNumberOfProps(out uint count);
        void GetProp(string name, out object prop);
    }
    [Guid("9C6B4CB0-23F8-49CC-A3ED-45A55000A6D2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDxDiagProvider
    {
        void Initialize(in DxDiagInitParams parameters);
        void GetRootContainer(out IDxDiagContainer instance);
    }
}