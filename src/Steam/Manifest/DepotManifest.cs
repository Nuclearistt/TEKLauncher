using System.IO.Compression;
using System.Security.Cryptography;
using Google.Protobuf;

namespace TEKLauncher.Steam.Manifest;

/// <summary>Represents a Steam depot manifest.</summary>
class DepotManifest
{
    /// <summary>File entries stored in the manifest.</summary>
    public readonly FileEntry[] Files;
    /// <summary>Initializes a new depot manifest object provided its file path and depot ID.</summary>
    /// <param name="path">Path to the manifest file, either compressed and protobuf-encoded or already decoded.</param>
    /// <param name="depotId">ID of the manifest's depot.</param>
    /// <exception cref="SteamException">An error occured when decoding or reading the manifest.</exception>
    public DepotManifest(string path, uint depotId)
    {
        Span<byte> buffer = stackalloc byte[4];
        if (path[^1] == 'c') //.manifest-enc
        {
            string writePath = Path.ChangeExtension(path, ".manifest-proto");
            try
            {
                using var zipFile = ZipFile.OpenRead(path);
                using var entryStream = zipFile.Entries[0].Open();
                using var writer = File.Create(writePath);
                entryStream.CopyTo(writer);
            }
            catch (InvalidDataException) { throw new SteamException(LocManager.GetString(LocCode.ManifestCorrupted)); }
            File.Delete(path);
            path = writePath;
            //Process the decompressed protobuf file further into an array of file entries
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
                if (stream.Read(buffer) < 4 || BitConverter.ToUInt32(buffer) != 0x71F617D0)
                    throw new SteamException(LocManager.GetString(LocCode.ManifestCorrupted));
                stream.Read(buffer);
                stream.SetLength(BitConverter.ToInt32(buffer) + 8); //To avoid copying the entire file into RAM
                var payload = new Payload();
                using (var protoDecoder = new CodedInputStream(stream))
                    protoDecoder.ReadRawMessage(payload);
                int numFiles = 0; //Files as opposed to directories
                foreach (var file in payload.Files)
                    if ((file.Flags & 64) == 0)
                        numFiles++;
                Files = new FileEntry[numFiles];
                using var aes = Aes.Create();
                aes.Key = Client.DepotKeys[depotId];
                byte[] iv = new byte[16];
                byte[] encryptedName = new byte[260];
                Span<byte> decryptedName = stackalloc byte[260];
                for (int i = 0, j = 0; i < payload.Files.Count; i++)
                {
                    var file = payload.Files[i];
                    if ((file.Flags & 64) != 0)
                        continue;
                    //Decrypt file name
                    if (!Convert.TryFromBase64String(file.Name, encryptedName, out int bytesDecoded))
                        throw new SteamException(LocManager.GetString(LocCode.ManifestCorrupted));
                    aes.DecryptEcb(new ReadOnlySpan<byte>(encryptedName, 0, 16), iv, PaddingMode.None);
                    aes.IV = iv;
                    using var transform = aes.CreateDecryptor();
                    using var cryptoStream = new CryptoStream(new MemoryStream(encryptedName, 16, bytesDecoded - 16), transform, CryptoStreamMode.Read);
                    int nameSize = 0;
                    while (bytesDecoded > 0)
                    {
                        bytesDecoded = cryptoStream.Read(decryptedName[nameSize..]);
                        nameSize += bytesDecoded;
                    }
                    string name = Encoding.UTF8.GetString(decryptedName[..--nameSize]);
                    //Copy chunks from RepeatedField into an array
                    var chunks = new ChunkEntry[file.Chunks.Count];
                    for (int k = 0; k < chunks.Length; k++)
                    {
                        var protoChunk = file.Chunks[k];
                        chunks[k] = new()
                        {
                            Gid = new(protoChunk.Gid.Memory),
                            Hash = protoChunk.Hash,
                            Offset = protoChunk.Offset,
                            CompressedSize = protoChunk.CompressedSize,
                            UncompressedSize = protoChunk.UncompressedSize
                        };
                    }
                    Array.Sort(chunks, (left, right) => left.Gid.CompareTo(right.Gid));
                    Files[j++] = new()
                    {
                        Name = name,
                        Size = file.Size,
                        Hash = new(file.Hash.Memory),
                        Chunks = chunks
                    };
                }
                if (depotId == 346110)
                    Files = Array.FindAll(Files, f => !f.Name.EndsWith(".uncompressed_size"));
                else if (depotId == 346111)
                    Files = Array.FindAll(Files, f =>
                        f.Name != @"ShooterGame\Binaries\Win64\officialservers.ini" &&
                        f.Name != @"ShooterGame\Binaries\Win64\news.ini" &&
                        f.Name != @"ShooterGame\Binaries\Win64\officialserverstatus.ini");
                Array.Sort(Files, (left, right) => left.Name.CompareTo(right.Name));
            }
            File.Delete(path);
            path = Path.ChangeExtension(path, ".manifest");
            //Write the processed array of file entries into a .manifest file
            {
                using var stream = File.Create(path);
                stream.Position = 4;
                BitConverter.TryWriteBytes(buffer, Files.Length);
                stream.Write(buffer);
                Span<byte> nameBuffer = stackalloc byte[260];
                Span<byte> chunkBuffer = nameBuffer[..40];
                Span<byte> fileBuffer = chunkBuffer[..32];
                foreach (var file in Files)
                {
                    int nameSize = Encoding.UTF8.GetBytes(file.Name, nameBuffer);
                    stream.WriteByte((byte)nameSize);
                    stream.Write(nameBuffer[..nameSize]);
                    BitConverter.TryWriteBytes(fileBuffer[..8], file.Size);
                    file.Hash.Data.Span.CopyTo(fileBuffer.Slice(8, 20));
                    BitConverter.TryWriteBytes(fileBuffer.Slice(28, 4), file.Chunks.Length);
                    stream.Write(fileBuffer);
                    foreach (var chunk in file.Chunks)
                    {
                        chunk.Gid.Data.Span.CopyTo(chunkBuffer);
                        BitConverter.TryWriteBytes(chunkBuffer.Slice(20, 4), chunk.Hash);
                        BitConverter.TryWriteBytes(chunkBuffer.Slice(24, 8), chunk.Offset);
                        BitConverter.TryWriteBytes(chunkBuffer.Slice(32, 4), chunk.CompressedSize);
                        BitConverter.TryWriteBytes(chunkBuffer.Slice(36, 4), chunk.UncompressedSize);
                        stream.Write(chunkBuffer);
                    }
                }
                stream.Position = 4;
                uint hash = CRC32.ComputeHash(stream);
                BitConverter.TryWriteBytes(buffer, hash);
                stream.Position = 0;
                stream.Write(buffer);
            }
        }
        else //.manifest
        {
            using var stream = File.OpenRead(path);
            if (stream.Read(buffer) < 4 || CRC32.ComputeHash(stream) != BitConverter.ToUInt32(buffer))
                throw new SteamException(LocManager.GetString(LocCode.ManifestCorrupted));
            stream.Position = 4;
            stream.Read(buffer);
            Files = new FileEntry[BitConverter.ToInt32(buffer)];
            Span<byte> nameBuffer = stackalloc byte[260];
            Span<byte> chunkBuffer = nameBuffer[..40];
            Span<byte> fileBuffer = chunkBuffer[..32];
            for (int i = 0; i < Files.Length; i++)
            {
                int nameSize = stream.ReadByte();
                stream.Read(nameBuffer[..nameSize]);
                string name = Encoding.UTF8.GetString(nameBuffer[..nameSize]);
                stream.Read(fileBuffer);
                long size = BitConverter.ToInt64(fileBuffer);
                var fileHash = new Hash(fileBuffer.Slice(8, 20).ToArray());
                var chunks = new ChunkEntry[BitConverter.ToInt32(fileBuffer.Slice(28, 4))];
                for (int j = 0; j < chunks.Length; j++)
                {
                    stream.Read(chunkBuffer);
                    var gid = new Hash(chunkBuffer[..20].ToArray());
                    uint chunkHash = BitConverter.ToUInt32(chunkBuffer.Slice(20, 4));
                    long offset = BitConverter.ToInt64(chunkBuffer.Slice(24, 8));
                    int compressedSize = BitConverter.ToInt32(chunkBuffer.Slice(32, 4));
                    int uncompressedSize = BitConverter.ToInt32(chunkBuffer.Slice(36, 4));
                    chunks[j] = new()
                    {
                        Gid = gid,
                        Hash = chunkHash,
                        Offset = offset,
                        CompressedSize = compressedSize,
                        UncompressedSize = uncompressedSize
                    };
                }
                Files[i] = new()
                {
                    Name = name,
                    Size = size,
                    Hash = fileHash,
                    Chunks = chunks
                };
            }
        }
    }
}