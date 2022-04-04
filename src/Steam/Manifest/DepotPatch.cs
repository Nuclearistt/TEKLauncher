using System.Security.Cryptography;
using Google.Protobuf;

namespace TEKLauncher.Steam.Manifest;

/// <summary>Represents a Steam depot patch.</summary>
class DepotPatch
{
    /// <summary>Patch chunk entries stored in the patch.</summary>
    public readonly PatchChunkEntry[] Chunks;
    /// <summary>Initializes a new depot patch object provided its file path and depot key.</summary>
    /// <param name="path">Path to the patch file, either encrypted and protobuf-encoded or already decoded.</param>
    /// <param name="depotKey">Decryption key for depot the patch belongs to.</param>
    /// <exception cref="SteamException">An error occured when decoding or reading the patch.</exception>
    public DepotPatch(string path, byte[] depotKey)
    {
        Span<byte> buffer = stackalloc byte[16];
        if (path[^1] == 'c') //.patch-enc
        {
            string writePath = Path.ChangeExtension(path, ".patch-proto");
            {
                using var stream = File.OpenRead(path);
                if (stream.Read(buffer) < 16)
                    throw new SteamException(LocManager.GetString(LocCode.PatchCorrupted));
                byte[] iv = new byte[16];
                using var aes = Aes.Create();
                aes.Key = depotKey;
                aes.DecryptEcb(buffer, iv, PaddingMode.None);
                aes.IV = iv;
                using var transform = aes.CreateDecryptor();
                using var cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read);
                using var writer = File.Create(writePath);
                cryptoStream.CopyTo(writer);
            }
            File.Delete(path);
            path = writePath;
            //Process the decrypted protobuf file further into an array of patch chunk entries
            {
                using var stream = File.OpenRead(path);
                if (stream.Read(buffer[..8]) < 8 || BitConverter.ToUInt32(buffer) != 0x502F15E5)
                    throw new SteamException(LocManager.GetString(LocCode.PatchCorrupted));
                byte[] protoBuffer = new byte[BitConverter.ToInt32(buffer[4..])];
                stream.Read(protoBuffer);
                var patch = new Patch();
                using var protoDecoder = new CodedInputStream(protoBuffer);
                protoDecoder.ReadRawMessage(patch);
                if (patch.DataAfterProto)
                    stream.Seek(4, SeekOrigin.Current);
                Chunks = new PatchChunkEntry[patch.Chunks.Count];
                for (int i = 0; i < Chunks.Length; i++)
                {
                    var protoChunk = patch.Chunks[i];
                    ReadOnlyMemory<byte> data;
                    if (patch.DataAfterProto)
                    {
                        byte[] dataBuffer = new byte[protoChunk.DataSize];
                        stream.Read(dataBuffer);
                        data = dataBuffer;
                    }
                    else
                        data = protoChunk.Data.Memory;
                    Chunks[i] = new()
                    {
                        SourceGid = new(protoChunk.SourceGid.Memory),
                        TargetGid = new(protoChunk.TargetGid.Memory),
                        Data = data
                    };
                }
            }
            File.Delete(path);
            path = Path.ChangeExtension(path, ".patch");
            //Write the processed array of patch chunk entries into a .patch file
            {
                using var stream = File.Create(path);
                stream.Position = 4;
                BitConverter.TryWriteBytes(buffer[..4], Chunks.Length);
                stream.Write(buffer[..4]);
                Span<byte> chunkBuffer = stackalloc byte[44];
                foreach (var chunk in Chunks)
                {
                    chunk.SourceGid.Data.Span.CopyTo(chunkBuffer[..20]);
                    chunk.TargetGid.Data.Span.CopyTo(chunkBuffer.Slice(20, 20));
                    BitConverter.TryWriteBytes(chunkBuffer.Slice(40, 4), chunk.Data.Length);
                    stream.Write(chunkBuffer);
                    stream.Write(chunk.Data.Span);
                }
                stream.Position = 4;
                uint hash = CRC32.ComputeHash(stream);
                BitConverter.TryWriteBytes(buffer[..4], hash);
                stream.Position = 0;
                stream.Write(buffer[..4]);
            }
        }
        else //.patch
        {
            using var stream = File.OpenRead(path);
            if (stream.Read(buffer[..4]) < 4 || CRC32.ComputeHash(stream) != BitConverter.ToUInt32(buffer))
                throw new SteamException(LocManager.GetString(LocCode.PatchCorrupted));
            stream.Position = 4;
            stream.Read(buffer[..4]);
            Chunks = new PatchChunkEntry[BitConverter.ToInt32(buffer)];
            Span<byte> chunkBuffer = stackalloc byte[44];
            for (int i = 0; i < Chunks.Length; i++)
            {
                stream.Read(chunkBuffer);
                var sourceGid = new Hash(chunkBuffer[..20].ToArray());
                var targetGid = new Hash(chunkBuffer.Slice(20, 20).ToArray());
                int dataSize = BitConverter.ToInt32(chunkBuffer[40..]);
                byte[] data = new byte[dataSize];
                stream.Read(data);
                Chunks[i] = new()
                {
                    SourceGid = sourceGid,
                    TargetGid = targetGid,
                    Data = data
                };
            }
        }
    }
}