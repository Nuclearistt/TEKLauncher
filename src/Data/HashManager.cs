using TEKLauncher.Servers;

namespace TEKLauncher.Data;

/// <summary>Manages SHA-1 hashes used for checking for game and DLC updates.</summary>
static class HashManager
{
    /// <summary>Value indicating whether the hashes are loaded.</summary>
    static bool _loaded;
    /// <summary>SHA-1 hashes of DLC .umap files.</summary>
    public static readonly Hash[] DLCHashes = new Hash[DLC.List.Length + 1]; //+ 1 is because of Genesis which consists of 2 map within one DLC
    /// <summary>SHA-1 hash of ShooterGame.exe.</summary>
    public static Hash GameHash { get; private set; }
    /// <summary>Attempts to downloads hashes unless they are loaded already.</summary>
    /// <returns><see langword="true"/> if hashes have been downloaded successfully or are already loaded; otherwise, <see langword="false"/>.</returns>
    public static bool Load()
    {
        if (_loaded)
            return true;
        byte[]? data = UdpClient.Transact(UdpClient.ArkoudaWatcherEndpoint, stackalloc byte[] { 0 });
        if (data is null || data.Length < (DLC.List.Length + 2) * 20)
            return false;
        GameHash = new(new(data, 0, 20));
        for (int i = 0; i < DLCHashes.Length; i++)
            DLCHashes[i] = new(new Span<byte>(data, (i + 1) * 20, 20).ToArray());
        _loaded = true;
        return true;
    }
}