using System.IO;
using System.Linq;
using System.Threading.Tasks;
using pdj.tiny7z.Compression;
using static System.BitConverter;
using static System.IO.File;
using static System.Text.Encoding;
using static System.Threading.Tasks.Task;

namespace TEKLauncher.Utils
{
    internal static class TEKArchive
    {
        private static bool DecompressArchive(object Args)
        {
            string[] ArgsArray = (string[])Args;
            return DecompressArchive(ArgsArray[0], ArgsArray[1]);
        }
        internal static bool DecompressArchive(string ArchivePath, string DestinationDirectory)
        {
            using (FileStream Reader = OpenRead(ArchivePath))
            {
                byte[] Buffer = new byte[4];
                Reader.Read(Buffer, 0, 4);
                using (CRC32 CRC = new CRC32())
                    if (!CRC.ComputeHash(Reader).SequenceEqual(Buffer))
                        return false;
                Reader.Position = 4L;
                using (MemoryStream Stream = new MemoryStream())
                {
                    DecompressSingleFile(Reader, Stream);
                    Stream.Position = 0L;
                    while (Stream.Position != Stream.Length)
                    {
                        int RPathSize = Stream.ReadByte();
                        byte[] RPath = new byte[RPathSize];
                        Stream.Read(RPath, 0, RPathSize);
                        string APath = $@"{DestinationDirectory}\{UTF8.GetString(RPath)}";
                        Directory.CreateDirectory(Path.GetDirectoryName(APath));
                        Stream.Read(Buffer, 0, 4);
                        using (FileStream Writer = Create(APath))
                        {
                            byte[] FileBuffer = new byte[ToUInt32(Buffer, 0)];
                            Stream.Read(FileBuffer, 0, FileBuffer.Length);
                            Writer.Write(FileBuffer, 0, FileBuffer.Length);
                        }
                    }
                }
            }
            return true;
        }
        internal static void DecompressSingleFile(Stream Input, Stream Output)
        {
            using (Lzma2DecoderStream Stream = new Lzma2DecoderStream(Input, (byte)Input.ReadByte(), long.MaxValue))
                Stream.CopyTo(Output);
        }
        internal static Task<bool> DecompressArchiveAsync(string ArchivePath, string DestinationDirectory) => Factory.StartNew(DecompressArchive, new[] { ArchivePath, DestinationDirectory });
    }
}