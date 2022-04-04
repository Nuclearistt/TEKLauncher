using System.IO.Compression;

namespace TEKLauncher.Utils;

/// <summary>Decoder for simple archive format based on Brotli compression algorithm.</summary>
static class BrotliArchive
{
    /// <summary>Decompresses an archive into specified folder.</summary>
    /// <param name="archivePath">Path to the .br file.</param>
    /// <param name="folderPath">Path to the folder that contents of the archive will be extracted to (must end with '\').</param>
    /// <param name="eventHandlers">Handlers for PrepareProgress and UpdateProgress events.</param>
    /// <returns><see langword="true"/> if decompression succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool Decompress(string archivePath, string folderPath, EventHandlers eventHandlers)
    {
        Span<byte> buffer = stackalloc byte[81920];
        using var stream = File.OpenRead(archivePath);
        stream.Read(buffer[..4]);
        if (CRC32.ComputeHash(stream) != BitConverter.ToUInt32(buffer))
            return false;
        stream.Position = 4;
        string intermediateFile = string.Concat(archivePath, "i");
        {
            using var decoderStream = new BrotliStream(stream, CompressionMode.Decompress);
            using var intermediateStream = File.Create(intermediateFile);
            decoderStream.CopyTo(intermediateStream);
            intermediateStream.Position = 0;
            eventHandlers.PrepareProgress?.Invoke(true, intermediateStream.Length);
            int pathSize;
            long progressAccumulator = 0;
            long lastRecordedTime = 0;
            while ((pathSize = intermediateStream.ReadByte()) > 0)
            {
                intermediateStream.Read(buffer[..pathSize]);
                string path = string.Concat(folderPath, Encoding.UTF8.GetString(buffer[..pathSize]));
                intermediateStream.Read(buffer[..4]);
                int size = BitConverter.ToInt32(buffer);
                progressAccumulator += pathSize + 5;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var writer = File.Create(path);
                while (size > 0)
                {
                    int bytesRead = 81920;
                    if (size < bytesRead)
                        bytesRead = size;
                    bytesRead = intermediateStream.Read(buffer[..bytesRead]);
                    size -= bytesRead;
                    writer.Write(buffer[..bytesRead]);
                    progressAccumulator += bytesRead;
                    long timeDifference = Environment.TickCount64 - lastRecordedTime;
                    if (timeDifference >= 200)
                    {
                        lastRecordedTime += timeDifference;
                        eventHandlers.UpdateProgress?.Invoke(progressAccumulator);
                        progressAccumulator = 0;
                    }
                }
            }
            eventHandlers.UpdateProgress?.Invoke(progressAccumulator);
        }
        File.Delete(intermediateFile);
        stream.Close();
        File.Delete(archivePath);
        return true;
    }
}