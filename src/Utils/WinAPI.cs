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
}