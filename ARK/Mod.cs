using System.Collections.Generic;
using System.IO;
using System.Linq;
using TEKLauncher.Utils.Zlib;
using TEKLauncher.Controls;
using TEKLauncher.Data;
using static System.BitConverter;
using static System.IO.File;
using static System.Text.Encoding;
using static System.Windows.Application;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.ARK
{
    internal class Mod
    {
        internal Mod(string Path, ulong[] SubscribedMods)
        {
            foreach (string File in Directory.EnumerateFiles(Path))
                if (File.EndsWith(".jpg") || File.EndsWith(".png"))
                    ImageFile = File;
            ModFilePath = $"{ModsPath = $@"{Game.Path}\ShooterGame\Content\Mods\{ID = ulong.Parse(Path.Substring(Path.LastIndexOf('\\') + 1))}"}.mod";
            IsInstalled = Directory.Exists(ModsPath) && FileExists(ModFilePath);
            IsSubscribed = SubscribedMods?.Contains(ID);
            string ModInfoFile = $@"{Path}\mod.info", OriginIDFile = $@"{Path}\OriginID.txt";
            OriginID = FileExists(OriginIDFile) ? ulong.Parse(ReadAllText(OriginIDFile).TrimEnd('\r', '\n')) : 0UL;
            if (FileExists(ModInfoFile))
                using (FileStream Stream = OpenRead(ModInfoFile))
                    Name = ReadString(Stream);
            else
                Name = string.Empty;
            this.Path = Path;
        }
        internal bool IsInstalled, UpdateAvailable;
        internal bool? IsSubscribed;
        internal ulong ID;
        internal string ImageFile, ModFilePath, ModsPath, Path;
        internal Status Status = Status.Installed;
        internal ModDetails Details, OriginDetails;
        internal readonly ulong OriginID;
        internal readonly string Name;
        internal void Install(Progress Progress, ProgressBar ProgressBar)
        {
            List<string> Names = new List<string>();
            using (FileStream Stream = OpenRead($@"{Path}\mod.info"))
            {
                ReadString(Stream);
                byte[] Buffer = new byte[4];
                Stream.Read(Buffer, 0, 4);
                int ItemsCount = ToInt32(Buffer, 0);
                for (int Iterator = 0; Iterator < ItemsCount; Iterator++)
                    Names.Add(ReadString(Stream));
            }
            Dictionary<string, string> Meta = new Dictionary<string, string>();
            using (FileStream Stream = OpenRead($@"{Path}\modmeta.info"))
            {
                byte[] Buffer = new byte[4];
                Stream.Read(Buffer, 0, 4);
                int ItemsCount = ToInt32(Buffer, 0);
                for (int Iterator = 0; Iterator < ItemsCount; Iterator++)
                    Meta[ReadString(Stream)] = ReadString(Stream);
            }
            DeletePath(ModsPath);
            DeletePath(ModFilePath);
            string FilesPath = $@"{Path}\WindowsNoEditor";
            IEnumerable<string> Files = Directory.EnumerateFiles(FilesPath, "*", SearchOption.AllDirectories);
            if (!(Progress is null))
            {
                Progress.Total = Files.Count();
                Current.Dispatcher.Invoke(ProgressBar.SetNumericMode);
            }
            foreach (string SourceFile in Files)
            {
                string DestinationFile = SourceFile.Replace(FilesPath, ModsPath);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DestinationFile));
                if (SourceFile.EndsWith(".z"))
                    using (FileStream Reader = OpenRead(SourceFile), Writer = Create(DestinationFile.Substring(0, DestinationFile.Length - 2)))
                    {
                        long Size = new ChunkMeta(Reader).UncompressedSize;
                        if (Size == 1641380927L)
                            Size = 131072L;
                        else if (Size == 0L)
                            Size = 1L;
                        int ChunksCount = (int)((new ChunkMeta(Reader).UncompressedSize + Size - 1L) / Size);
                        ChunkMeta[] ChunkSizes = new ChunkMeta[ChunksCount];
                        for (int Iterator = 0; Iterator < ChunksCount; Iterator++)
                            ChunkSizes[Iterator] = new ChunkMeta(Reader);
                        foreach (ChunkMeta ChunkSize in ChunkSizes)
                            new ZlibDecompressor(Reader, Writer, ChunkSize.CompressedSize, ChunkSize.UncompressedSize).DecompressChunk();
                    }
                else if (!SourceFile.EndsWith(".uncompressed_size"))
                    Copy(SourceFile, DestinationFile, true);
                if (!(Progress is null))
                    Progress.Increase();
            }
            using (FileStream Stream = Create(ModFilePath))
            {
                Stream.Write(GetBytes(ID), 0, 8);
                WriteString(Stream, Details.Status == 1 ? Details.Name : "ModName");
                WriteString(Stream, $"../../../ShooterGame/Content/Mods/{ID}");
                Stream.Write(GetBytes(Names.Count), 0, 4);
                foreach (string Name in Names)
                    WriteString(Stream, Name);
                Stream.Write(GetBytes(4280483635U), 0, 4);
                Stream.Write(GetBytes(2), 0, 4);
                Stream.WriteByte((byte)(Meta.ContainsKey("ModType") ? int.Parse(Meta["ModType"]) : 0));
                Stream.Write(GetBytes(Meta.Count), 0, 4);
                foreach (KeyValuePair<string, string> Item in Meta)
                {
                    WriteString(Stream, Item.Key);
                    WriteString(Stream, Item.Value);
                }
            }
            IsInstalled = true;
        }
        internal void Uninstall(bool DeleteFromWorkshop = true, bool DeleteFromMods = true)
        {
            if (DeleteFromWorkshop)
                DeletePath(Path);
            if (DeleteFromMods && IsInstalled)
            {
                DeletePath(ModsPath);
                DeletePath(ModFilePath);
            }
        }
        private static string ReadString(FileStream Stream)
        {
            byte[] Buffer = new byte[4];
            Stream.Read(Buffer, 0, 4);
            int StringSize = ToInt32(Buffer, 0);
            if (StringSize == 0)
                return null;
            Stream.Read(Buffer = new byte[StringSize], 0, StringSize);
            return UTF8.GetString(Buffer, 0, --StringSize);
        }
        private static void WriteString(FileStream Stream, string String)
        {
            byte[] EncodedString = UTF8.GetBytes(String ?? string.Empty);
            Stream.Write(GetBytes(EncodedString.Length + 1), 0, 4);
            Stream.Write(EncodedString, 0, EncodedString.Length);
            Stream.WriteByte(0);
        }
        private struct ChunkMeta
        {
            internal ChunkMeta(FileStream Stream)
            {
                byte[] Buffer = new byte[8];
                Stream.Read(Buffer, 0, 8);
                CompressedSize = ToInt64(Buffer, 0);
                Stream.Read(Buffer, 0, 8);
                UncompressedSize = ToInt64(Buffer, 0);
            }
            internal readonly long CompressedSize, UncompressedSize;
        }
    }
}