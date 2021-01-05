using System;
using System.Collections.Generic;
using System.IO;
using static System.Convert;
using static System.IO.File;
using static System.Text.Encoding;
using static TEKLauncher.ARK.CreamAPI;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.SteamInterop.Network.ContentDownloader;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.SteamInterop.Network.Manifest
{
    internal class DepotManifest
    {
        internal DepotManifest(string ManifestPath, uint DepotID)
        {
            Path = ManifestPath;
            using (FileStream Stream = OpenRead(ManifestPath))
            using (BinaryReader Reader = new BinaryReader(Stream))
            {
                if (Stream.Length < 4)
                    throw new ValidatorException(LocString(LocCode.ManifestCorrupted));
                if (Reader.ReadUInt32() == 0x71F617D0U)
                {
                    Payload Payload = new Payload();
                    try { Payload.Deserialize(Stream, Reader.ReadUInt32()); }
                    catch (IOException) { throw new ValidatorException(LocString(LocCode.ManifestCorrupted)); }
                    Files = Payload.Files;
                    byte[] DepotKey = DepotKeys[DepotID];
                    for (int Iterator = 0; Iterator < Files.Count; Iterator++)
                    {
                        FileEntry File = Files[Iterator];
                        byte[] DecryptedName, EncryptedName = FromBase64String(File.Name);
                        try { DecryptedName = AESDecrypt(EncryptedName, DepotKey); }
                        catch { throw new ValidatorException(LocString(LocCode.ManifestDecryptFailed)); }
                        File.Name = UTF8.GetString(DecryptedName).TrimEnd('\0');
                        if (IsInstalled && File.Name.Contains("steam_api64") || File.Name.EndsWith(".uncompressed_size"))
                            Files.RemoveAt(Iterator--);
                        else
                            Files[Iterator] = File;
                    }
                    Files.Sort(Comparator);
                }
                else
                    throw new ValidatorException(LocString(LocCode.ManifestCorrupted));
            }
        }
        internal readonly string Path;
        internal readonly List<FileEntry> Files;
        private static readonly Comparison<FileEntry> Comparator = (A, B) => A.Name.CompareTo(B.Name);
    }
}