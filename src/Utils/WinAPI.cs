using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace TEKLauncher.Utils;

/// <summary>Consolidates function imports from Windows APIs and util methods using them.</summary>
static class WinAPI
{
    #region kernel32.dll
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern bool GetDiskFreeSpaceExW(string lpDirectoryName, out long lpFreeBytesAvailableToCaller, out long lpTotalNumberOfBytes, out long lpTotalNumberOfFreeBytes);
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
    /// <summary>Retrieves the amount of free space on specified disk.</summary>
    /// <param name="directory">A directory on the disk whose free space amount will be retrieved.</param>
    /// <returns>The amount of disk free space in bytes.</returns>
    public static long GetDiskFreeSpace(string directory) => GetDiskFreeSpaceExW(directory, out long freeSpace, out _, out _) ? freeSpace : 0;
    [ComImport]
    [Guid("A65B8071-3BFE-4213-9A5B-491DA4461CA7")]
    public class DxDiagProvider { }

    [Guid("9C6B4CB0-23F8-49CC-A3ED-45A55000A6D2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDxDiagProvider
    {
        void Initialize(ref DXDIAG_INIT_PARAMS pParams);
        void GetRootContainer(out IDxDiagContainer ppInstance);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DXDIAG_INIT_PARAMS
    {
        public int dwSize;
        public uint dwDxDiagHeaderVersion;
        public bool bAllowWHQLChecks;
        public IntPtr pReserved;
    };
    [Guid("7D0F462F-4064-4862-BC7F-933E5058C10F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDxDiagContainer
    {
        void EnumChildContainerNames(uint dwIndex, string pwszContainer, uint cchContainer);
        void EnumPropNames(uint dwIndex, string pwszPropName, uint cchPropName);
        void GetChildContainer(string pwszContainer, out IDxDiagContainer ppInstance);
        void GetNumberOfChildContainers(out uint pdwCount);
        void GetNumberOfProps(out uint pdwCount);
        void GetProp(string pwszPropName, out object pvarProp);
    }
    private static T GetProperty<T>(IDxDiagContainer container, string propName)
    {
        container.GetProp(propName, out object variant);
        return (T)Convert.ChangeType(variant, typeof(T));
    }
    public static int GetDirectXMajorVersion()
    {
        int result = 0;
        IDxDiagProvider? provider = null;
        IDxDiagContainer? rootContainer = null;
        IDxDiagContainer? systemInfoContainer = null;
        try
        {
            provider = (IDxDiagProvider)new DxDiagProvider();
            DXDIAG_INIT_PARAMS initParams = new DXDIAG_INIT_PARAMS
            {
                dwSize = Marshal.SizeOf<DXDIAG_INIT_PARAMS>(),
                dwDxDiagHeaderVersion = 111
            };
            provider.Initialize(ref initParams);

            provider.GetRootContainer(out rootContainer);
            rootContainer.GetChildContainer("DxDiag_SystemInfo", out systemInfoContainer);

            result = GetProperty<int>(systemInfoContainer, "dwDirectXVersionMajor");
        }
        finally
        {
            if (systemInfoContainer != null)
                Marshal.ReleaseComObject(systemInfoContainer);
            if (rootContainer != null)
                Marshal.ReleaseComObject(rootContainer);
            if (provider != null)
                Marshal.ReleaseComObject(provider);
        }
        return result;
    }
}