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
    static extern IntPtr GetModuleHandleW(string moduleName);
    [DllImport("kernel32.dll")]
    static extern IntPtr GetProcAddress(IntPtr module, string procName);
    [DllImport("kernel32.dll")]
    static extern IntPtr VirtualAlloc(IntPtr lpAddress, ulong dwSize, uint flAllocationType, uint flProtect);
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
    /// <summary>Loads PE image into process memory.</summary>
    /// <param name="imageData">PE file binary data.</param>
    /// <param name="entryPoint">When the function returns, contains the pointer to image's entry point function specified in its optional header.</param>
    /// <param name="imageSize">When the function returns, contains the size of loaded image in size.</param>
    /// <returns>Pointer to image memory region with read, write and execute access.</returns>
    public static IntPtr LoadPeImage(byte[] imageData, out IntPtr entryPoint, out ulong imageSize)
    {
        //Read required offsets from the header
        int peHeaderRva = BitConverter.ToInt32(imageData, 0x3C);
        int numSections = BitConverter.ToUInt16(imageData, peHeaderRva + 6);
        int entryPointRva = BitConverter.ToInt32(imageData, peHeaderRva + 40);
        imageSize = (ulong)BitConverter.ToInt32(imageData, peHeaderRva + 80);
        int headersSize = BitConverter.ToInt32(imageData, peHeaderRva + 84);
        int importTableRva = BitConverter.ToInt32(imageData, peHeaderRva + 144);
        int sectionHeaderRva = peHeaderRva + 136 + BitConverter.ToInt32(imageData, peHeaderRva + 132) * 8;
        //Allocate memory region for the image
        IntPtr regionBase = VirtualAlloc(IntPtr.Zero, imageSize, 0x3000, 0x40);
        //Copy headers
        Marshal.Copy(imageData, 0, regionBase, headersSize);
        //Copy sections
        for (int i = 0; i < numSections; i++)
        {
            int rawAddress = BitConverter.ToInt32(imageData, sectionHeaderRva + 20);
            int virtualAddress = BitConverter.ToInt32(imageData, sectionHeaderRva + 12);
            int size = BitConverter.ToInt32(imageData, sectionHeaderRva + 16);
            Marshal.Copy(imageData, rawAddress, regionBase + virtualAddress, size);
            sectionHeaderRva += 40;
        }
        //Load addresses of functions imported from DLLs
        for(;;)
        {
            int nameRva = Marshal.ReadInt32(regionBase + importTableRva + 12);
            if (nameRva == 0)
                break;
            string dllName = Marshal.PtrToStringAnsi(regionBase + nameRva)!;
            IntPtr module = GetModuleHandleW(dllName);
            int iltRva = Marshal.ReadInt32(regionBase + importTableRva);
            int iatRva = Marshal.ReadInt32(regionBase + importTableRva + 16);
            long iltEntry;
            while ((iltEntry = Marshal.ReadInt64(regionBase + iltRva)) != 0)
            {
                string functionName = Marshal.PtrToStringAnsi(regionBase + (int)iltEntry + 2)!;
                Marshal.WriteIntPtr(regionBase + iatRva, GetProcAddress(module, functionName));
                iltRva += 8;
                iatRva += 8;
            }
            importTableRva += 20;
        }
        entryPoint = regionBase + entryPointRva;
        return regionBase;
    }
}