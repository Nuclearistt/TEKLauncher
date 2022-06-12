using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip.Compression;
using TEKLauncher.Controls;
using TEKLauncher.Servers;
using TEKLauncher.Steam.Manifest;
using TEKLauncher.Tabs;
using TEKLauncher.Windows;

namespace TEKLauncher.Steam;

/// <summary>Main Steam client class, manages updates and validations.</summary>
static class Client
{
    /// <summary>Depots' AES decryption keys.</summary>
    public static readonly Dictionary<uint, byte[]> DepotKeys = new()
    {
        [346110] = new byte[]
        {
            0xE1, 0x6F, 0xF7, 0xF0, 0x82, 0x25, 0xD9, 0xAE, 0x36, 0x35, 0xA9, 0x88, 0x33, 0xA1, 0xC6, 0x3A,
            0xCC, 0x58, 0xB2, 0xA1, 0x04, 0xB0, 0xB3, 0x7A, 0x96, 0x05, 0xD4, 0x94, 0x68, 0x37, 0x87, 0xBE
        },
        [346111] = new byte[]
        {
            0x5E, 0xDB, 0x30, 0x79, 0x72, 0xCD, 0x5B, 0xF1, 0xE3, 0x08, 0x1A, 0xED, 0xC9, 0x86, 0xEF, 0x72,
            0x1D, 0xFD, 0x27, 0xCA, 0xE1, 0x6D, 0x0A, 0x97, 0x6C, 0x6B, 0x7E, 0xA6, 0xE8, 0xFF, 0x20, 0x89
        },
        [346114] = new byte[]
        {
            0xD8, 0xD4, 0x9A, 0xB9, 0x8F, 0x0F, 0x75, 0x30, 0xC0, 0xC2, 0x92, 0x62, 0x13, 0xC7, 0xAC, 0x64,
            0x62, 0x57, 0x12, 0xA2, 0xEF, 0xBB, 0xC9, 0x6E, 0x6B, 0x3F, 0x06, 0x94, 0x37, 0x41, 0xF8, 0x06
        },
        [375351] = new byte[]
        {
            0x80, 0x57, 0x31, 0x13, 0xA7, 0xBF, 0x29, 0x45, 0x55, 0xB4, 0xC8, 0xE4, 0xE0, 0x41, 0xC6, 0x9E,
            0x7A, 0x5A, 0x69, 0x52, 0x7A, 0x4B, 0x65, 0xD3, 0xCE, 0x6F, 0x47, 0xC2, 0x38, 0x24, 0x88, 0xA7
        },
        [375354] = new byte[]
        {
            0xE2, 0xFA, 0x71, 0xE0, 0x18, 0x10, 0xBB, 0xD5, 0x54, 0xE9, 0x47, 0x13, 0xE8, 0x4E, 0xFB, 0x08,
            0x4E, 0x0A, 0x99, 0xFF, 0x78, 0xF2, 0x1F, 0xBA, 0x3C, 0x20, 0x14, 0x10, 0xF7, 0x4E, 0x9A, 0x19
        },
        [375357] = new byte[]
        {
            0x9B, 0xC4, 0x5B, 0x9B, 0x59, 0xF3, 0xF3, 0x21, 0x54, 0xEE, 0x76, 0xB4, 0x7F, 0xF8, 0xB6, 0x01,
            0x5E, 0x92, 0xD5, 0xA6, 0x34, 0x2C, 0x44, 0x4B, 0xED, 0x3E, 0xB2, 0x10, 0xBC, 0xD2, 0x4B, 0x60
        },
        [473851] = new byte[]
        {
            0x99, 0xFB, 0x59, 0x71, 0x07, 0x88, 0x29, 0x42, 0xCA, 0x94, 0xE2, 0x03, 0x82, 0x15, 0xF0, 0x3F,
            0x87, 0x07, 0xD5, 0x7B, 0x05, 0x94, 0x60, 0xEC, 0x6B, 0xDE, 0x82, 0x65, 0xBF, 0x02, 0x07, 0x33
        },
        [473854] = new byte[]
        {
            0xDA, 0xC7, 0x1F, 0x84, 0xF6, 0xDD, 0x94, 0x0B, 0x7B, 0xFE, 0x12, 0xE5, 0xDF, 0x2B, 0xB9, 0x8E,
            0xFF, 0x8B, 0xA6, 0x22, 0xD7, 0x6C, 0x30, 0x09, 0x1C, 0x52, 0xEC, 0xFD, 0x60, 0xBB, 0xF3, 0x34
        },
        [473857] = new byte[]
        {
            0xC9, 0xBB, 0x21, 0x64, 0x42, 0x70, 0xFD, 0x68, 0x83, 0xBC, 0x3F, 0x00, 0x7A, 0xA6, 0x6F, 0x62,
            0x5E, 0xA3, 0x28, 0x1A, 0xD2, 0x19, 0x69, 0x00, 0xDF, 0xBB, 0xDF, 0x12, 0xCE, 0xB5, 0x8C, 0xE2
        },
        [1318685] = new byte[]
        {
            0x32, 0x3F, 0x24, 0x3A, 0x7C, 0xED, 0x4F, 0x90, 0x62, 0x8B, 0xA9, 0xDA, 0x90, 0x1C, 0xA9, 0x63,
            0x02, 0x35, 0x0C, 0x23, 0xC6, 0x25, 0x15, 0x3D, 0x21, 0x42, 0x5D, 0x93, 0xDF, 0x0A, 0x62, 0xCF
        },
        [1691801] = new byte[]
        {
            0xDB, 0x89, 0xFE, 0x99, 0x71, 0x0B, 0xE8, 0xD8, 0x0B, 0x85, 0x37, 0x7D, 0x37, 0x21, 0x04, 0x87,
            0xDB, 0xCE, 0x8B, 0x9B, 0xFB, 0x8E, 0x90, 0x76, 0x7F, 0x97, 0xE1, 0x38, 0xD3, 0xE0, 0x20, 0x6E
        },
        [1887561] = new byte[]
        {
            0xA3, 0xF5, 0xAA, 0x53, 0xC1, 0x6A, 0xF3, 0x24, 0xC6, 0xB5, 0x73, 0x26, 0x57, 0x8C, 0xE7, 0xA4,
            0x34, 0x1C, 0x7A, 0xEF, 0x7A, 0x14, 0xFF, 0x52, 0xCC, 0xFA, 0x35, 0xCB, 0xA9, 0xFC, 0xCB, 0xC7
        }
    };
    /// <summary>Cached latest manifest IDs of game and DLC depots.</summary>
    public static readonly Dictionary<uint, ulong> DepotManifestIds = new()
    {
        [346111] = 0,
        [346114] = 0,
        [375351] = 0,
        [375354] = 0,
        [375357] = 0,
        [473851] = 0,
        [473854] = 0,
        [473857] = 0,
        [1318685] = 0,
        [1691801] = 0,
        [1887561] = 0
    };
    /// <summary>Known currently installed depot manifest IDs.</summary>
    public static readonly Dictionary<ItemIdentifier, ulong> CurrentManifestIds = new();
    /// <summary>Gets or sets the number of threads that will be used for download tasks.</summary>
    public static int NumberOfDownloadThreads { get; set; } = 20;
    /// <summary>Gets or sets the last time <see cref="DepotManifestIds"/> were updated.</summary>
    public static long ManifestIdsLastUpdated { get; set; }
    /// <summary>Gets or sets path to the folder where downloads are stored.</summary>
    public static string DownloadsFolder { get; set; } = string.Empty;
    /// <summary>Gets or sets path to the folder where manifests are stored.</summary>
    public static string ManifestsFolder { get; set; } = string.Empty;
    /// <summary>Download thread procedure.</summary>
    /// <param name="obj">Thread parameter, for this method it's a tuple of <see cref="DownloadContext"/> and <see cref="int"/> (thread index).</param>
    static void Download(object? obj)
    {
        if (obj is not (DownloadContext context, int index))
            throw new ArgumentException("Download thread parameter must be a tuple of DownloadContext and int", nameof(obj));
        string basePath = $@"{DownloadsFolder}\{context.Item}\";
        FileStream? stream = null;
        bool createNewStream = true;
        using var httpClient = new HttpClient
        {
            BaseAddress = CDNClient.Servers[index],
            DefaultRequestVersion = new(1, 1),
            Timeout = TimeSpan.FromSeconds(10)
        };
        using var aes = Aes.Create();
        aes.Key = DepotKeys[context.Item.DepotId];
        Utils.LZMA.Decoder lzmaDecoder = new();
        int deltaIndex = -1;
        int chunkIndex = 0;
        string baseRequestUrl = $"depot/{context.Item.DepotId}/chunk/";
        Span<char> requestUrl = stackalloc char[baseRequestUrl.Length + 40];
        baseRequestUrl.CopyTo(requestUrl);
        byte[] encryptedChunkBuffer = new byte[2097152];
        Span<byte> decryptedChunkBuffer = stackalloc byte[2097152];
        Span<byte> uncompressedChunkBuffer = stackalloc byte[1048576];
        byte[] iv = new byte[16];
        int numRetryAttempts = 5 + NumberOfDownloadThreads;
        try
        {
            for(;;)
            {
                //Check if any other download thread threw an exception
                if (context.Exception is not null)
                {
                    lock (context)
                    {
                        if (deltaIndex < context.DeltaIndex && deltaIndex >= 0)
                        {
                            context.DeltaIndex = deltaIndex;
                            context.ChunkIndex = chunkIndex;
                        }
                        else if (deltaIndex == context.DeltaIndex && chunkIndex < context.ChunkIndex)
                            context.ChunkIndex = chunkIndex;
                    }
                    break;
                }
                if (context.CancellationToken.IsCancellationRequested)
                    break;
                //Determine the delta and its chunk to download in current iteration
                DeltaFile delta;
                ChunkEntry chunk;
                lock (context)
                {
                    if (context.DeltaIndex != deltaIndex)
                    {
                        if (deltaIndex < 0)
                            deltaIndex = 0;
                        createNewStream = true;
                        for (; context.DeltaIndex < context.Deltas.Length && context.Deltas[context.DeltaIndex].Chunks.Length == 0; context.DeltaIndex++);
                        deltaIndex = context.DeltaIndex;
                    }
                    if (deltaIndex >= context.Deltas.Length)
                        break;
                    delta = context.Deltas[deltaIndex];
                    chunkIndex = context.ChunkIndex++;
                    chunk = delta.Chunks[chunkIndex];
                    if (context.ChunkIndex >= delta.Chunks.Length)
                    {
                        context.DeltaIndex++;
                        context.ChunkIndex = 0;
                    }
                } 
                chunk.Gid.WriteTo(requestUrl[^40..]);
                string url = new(requestUrl);
                bool success = false;
                var messageCode = LocCode.NA;
                //Attempt to do the download cycle multiple times if needed
                for (int i = 0; i < numRetryAttempts; i++)
                {
                    int offset = 0;
                    //Download the encrypted chunk data
                    try
                    {
                        using var downloadStream = httpClient.GetStreamAsync(i > 5 ? string.Concat(CDNClient.Servers[i - 5].ToString(), url) : url).Result;
                        int bytesRead;
                        do
                        {
                            var readTask = downloadStream.ReadAsync(encryptedChunkBuffer, offset, chunk.CompressedSize - offset);
                            if (Task.WaitAny(readTask, Task.Delay(5000)) == 1)
                            {
                                offset = int.MaxValue;
                                break;
                            }
                            bytesRead = readTask.Result;
                            offset += bytesRead;
                        }
                        while (bytesRead > 0);
                    }
                    catch (Exception e) when (e is TaskCanceledException || e is AggregateException && e.InnerException is TaskCanceledException)
                    {
                        messageCode = LocCode.DownloadTimeout;
                        continue;
                    }
                    catch
                    {
                        messageCode = LocCode.DownloadFailed;
                        continue;
                    }
                    if (offset == int.MaxValue)
                    {
                        messageCode = LocCode.DownloadTimeout;
                        continue;
                    }
                    if (offset != chunk.CompressedSize)
                    {
                        messageCode = LocCode.IncompleteDownload;
                        continue;
                    }
                    //Decrypt the chunk data
                    try
                    {
                        aes.DecryptEcb(new ReadOnlySpan<byte>(encryptedChunkBuffer, 0, 16), iv, PaddingMode.None);
                        aes.IV = iv;
                        using var transform = aes.CreateDecryptor();
                        using var cryptoStream = new CryptoStream(new MemoryStream(encryptedChunkBuffer, 16, chunk.CompressedSize - 16), transform, CryptoStreamMode.Read);
                        offset = 0;
                        int bytesRead;
                        do
                        {
                            bytesRead = cryptoStream.Read(decryptedChunkBuffer[offset..]);
                            offset += bytesRead;
                        }
                        while (bytesRead > 0);
                    }
                    catch
                    {
                        messageCode = LocCode.DecryptionFailure;
                        continue;
                    }
                    //Decompress the chunk data
                    if (!lzmaDecoder.Decode(decryptedChunkBuffer[..offset], uncompressedChunkBuffer[..chunk.UncompressedSize], default))
                    {
                        messageCode = LocCode.DecompressionFailure;
                        continue;
                    }
                    //Compute Adler hash of chunk data and ensure that it's correct
                    if (ComputeAdlerHash(uncompressedChunkBuffer[..chunk.UncompressedSize]) != chunk.Hash)
                    {
                        messageCode = LocCode.AdlerHashMismatch;
                        continue;
                    }
                    success = true;
                    break;
                }
                if (!success)
                    throw new SteamException(LocManager.GetString(messageCode));
                //(Re)open file stream if a switch to another file has occurred
                if (createNewStream)
                {
                    stream?.Dispose();
                    stream = new FileStream(string.Concat(basePath, delta.Name), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    createNewStream = false;
                }
                //Compute chunk's offset within the file
                if (delta.Incomplete)
                {
                    long position = 0;
                    for (int i = 0; i < chunkIndex; i++)
                        position += delta.Chunks[i].UncompressedSize;
                    stream!.Position = position;
                }
                else
                    stream!.Position = chunk.Offset;
                //Write chunk data to the file
                stream.Write(uncompressedChunkBuffer[..chunk.UncompressedSize]);
                Interlocked.Add(ref context.Progress, chunk.CompressedSize);
            }
        }
        catch (Exception e)
        {
            lock (context)
            {
                context.Exception = e;
                if (deltaIndex < context.DeltaIndex && deltaIndex >= 0)
                {
                    context.DeltaIndex = deltaIndex;
                    context.ChunkIndex = chunkIndex;
                }
                else if (deltaIndex == context.DeltaIndex && chunkIndex < context.ChunkIndex)
                    context.ChunkIndex = chunkIndex;
            }
        }
        stream?.Dispose();
    }
    /// <summary>Verifies installed files of specified item.</summary>
    /// <param name="item">Item to verify.</param>
    /// <param name="files">Depot manifest file entries for the item.</param>
    /// <param name="deltas">List that will receive deltas for files/chunks that need to be downloaded.</param>
    /// <param name="context">Reference to the context of this validation.</param>
    /// <exception cref="AggregateException">An error occurred during validation.</exception>
    static void Validate(ItemIdentifier item, FileEntry[] files, List<DeltaFile> deltas, ref ValidationContext context)
    {
        string basePath = string.Concat(item.DepotId == 346110 ? $@"{Mod.CompressedModsDirectory}\{item.ModId}" : Game.Path, "\\");
        var chunks = new List<ChunkEntry>(); //Created before for loop to avoid unnecessary memory (re-)allocations
        Span<byte> buffer = stackalloc byte[1048576];
        var hash = new Hash.StackHash(buffer[..20]);
        for (; context.Index < files.Length; context.Index++)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;
            var file = files[context.Index];
            string localFile = string.Concat(basePath, file.Name);
            if (file.Size == 0)
            {
                if (File.Exists(localFile))
                    context.FilesUpToDate++;
                else
                {
                    context.FilesMissing++;
                    deltas.Add(new()
                    {
                        Name = file.Name,
                        Incomplete = false,
                        FileSize = 0,
                        ChunksSize = 0,
                        Chunks = Array.Empty<ChunkEntry>()
                    });
                }
                continue;
            }
            if (!File.Exists(localFile))
            {
                deltas.Add(new()
                {
                    Name = file.Name,
                    Incomplete = false,
                    FileSize = file.Size,
                    ChunksSize = file.Size,
                    Chunks = file.Chunks
                });
                context.Progress += file.Size;
                context.FilesMissing++;
                continue;
            }
            using var stream = File.OpenRead(localFile);
            long fileSize = stream.Length;
            if (fileSize == file.Size)
            {
                Utils.SHA1.ComputeHash(stream, buffer, ref context.Progress);
                if (hash == file.Hash)
                {
                    context.FilesUpToDate++;
                    continue;
                }
            }
            context.Progress -= file.Size;
            long chunksSize = 0;
            foreach (var chunk in file.Chunks)
            {
                if (chunk.Offset + chunk.UncompressedSize > fileSize)
                {
                    chunks.Add(chunk);
                    chunksSize += chunk.UncompressedSize;
                }
                else
                {
                    stream.Position = chunk.Offset;
                    stream.Read(buffer[..chunk.UncompressedSize]);
                    if (ComputeAdlerHash(buffer[..chunk.UncompressedSize]) != chunk.Hash)
                    {
                        chunks.Add(chunk);
                        chunksSize += chunk.UncompressedSize;
                    }
                }
                context.Progress += chunk.UncompressedSize;
            }
            if (chunks.Count == 0) //File data is okay but its size in file system is bigger than defined in the manifest, so it should be cropped
            {
                stream.Close();
                using var setLengthStream = File.OpenWrite(localFile);
                setLengthStream.SetLength(file.Size);
                context.FilesUpToDate++;
                continue;
            }
            deltas.Add(new()
            {
                Name = file.Name,
                Incomplete = chunks.Count != file.Chunks.Length,
                FileSize = file.Size,
                ChunksSize = chunksSize,
                Chunks = chunks.ToArray()
            });
            chunks.Clear();
            context.FilesOutdated++;
        }
    }
    /// <summary>Writes a string to specified file stream in UE4 format.</summary>
    /// <param name="stream">File stream to write string to.</param>
    /// <param name="str">String to write.</param>
    static void WriteString(FileStream stream, string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            stream.Write(stackalloc byte[5] { 0x01, 0x00, 0x00, 0x00, 0x00 });
            return;
        }
        int stringSize = Encoding.UTF8.GetByteCount(str) + 1;
        Span<byte> buffer = stackalloc byte[4];
        BitConverter.TryWriteBytes(buffer, stringSize);
        stream.Write(buffer);
        buffer = stackalloc byte[stringSize];
        buffer[^1] = 0x00;
        Encoding.UTF8.GetBytes(str, buffer);
        stream.Write(buffer);
    }
    /// <summary>Computes Adler hash for specified span of bytes.</summary>
    /// <param name="data">Span of bytes to compute the hash for.</param>
    /// <returns>The Adler hash value.</returns>
    static uint ComputeAdlerHash(Span<byte> data)
    {
        uint a = 0;
        uint b = 0;
        foreach (byte i in data)
        {
            a += i;
            a %= 65521;
            b += a;
            b %= 65521;
        }
        return a | (b << 16);
    }
    /// <summary>Reads an UE4-format string from specified file stream.</summary>
    /// <param name="stream">File stream to read string from.</param>
    /// <returns>The string that was read.</returns>
    static string ReadString(FileStream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        stream.Read(buffer);
        int stringSize = BitConverter.ToInt32(buffer);
        if (stringSize == 0)
            return string.Empty;
        buffer = stackalloc byte[--stringSize];
        stream.Read(buffer);
        stream.ReadByte();
        return Encoding.UTF8.GetString(buffer);
    }
    /// <summary>Computes the difference between file lists of two manifests.</summary>
    /// <param name="context">Context of the parent task. Its <see cref="TaskContext.SourceManifest"/> and <see cref="TaskContext.TargetManifest"/> are compared.</param>
    /// <returns>An array of delta file entries that include new files and new chunks within existing files; <see langword="null"/> if no new files/chunks were found.</returns>
    static DeltaFile[]? ComputeDeltas(ref TaskContext context)
    {
        var removed = new List<string>();
        var deltas = new List<DeltaFile>();
        var relocs = new List<Relocation>();
        var chunks = new List<ChunkEntry>();
        var relocEntries = new List<Relocation.Entry>();
        int i = 0, offset = 0; //offset allows comparing identical files in both manifests without doing an extensive search in every iteration
        for (; i < context.SourceManifest!.Files.Length && i + offset < context.TargetManifest!.Files.Length; i++) //Range intersecting both manifests
        {
            var sourceFile = context.SourceManifest.Files[i];
            var targetFile = context.TargetManifest.Files[i + offset];
            int difference = sourceFile.Name.CompareTo(targetFile.Name);
            if (difference < 0) //A file that is present in source manifest got removed from target one
            {
                removed.Add(sourceFile.Name);
                offset--;
            }
            else if (difference > 0) //A new file was added to target manifest
            {
                deltas.Add(new()
                {
                    Name = targetFile.Name,
                    Incomplete = false,
                    FileSize = targetFile.Size,
                    ChunksSize = targetFile.Size,
                    Chunks = targetFile.Chunks
                });
                foreach (var chunk in targetFile.Chunks)
                    context.DownloadSize += chunk.CompressedSize;
                context.InstallSize += targetFile.Size;
                offset++;
                i--;
            }
            else if (sourceFile.Hash != targetFile.Hash) //A file got updated
            {
                int j = 0, chunkOffset = 0; //chunkOffset has the same purpose as offset but for chunks
                long chunksSize = 0;
                for (; j < sourceFile.Chunks.Length && j + chunkOffset < targetFile.Chunks.Length; j++) //Range intersecting both files
                {
                    var sourceChunk = sourceFile.Chunks[j];
                    var targetChunk = targetFile.Chunks[j + chunkOffset];
                    int chunkDifference = sourceChunk.Gid.CompareTo(targetChunk.Gid);
                    if (chunkDifference < 0) //A chunk that got removed in target version of the file
                        chunkOffset--;
                    else if (chunkDifference > 0) //A new chunk that was added to the file or patched from previous version
                    {
                        //Only chunks that are not result of patching a chunk from previous version are counted
                        //Unfortunately the entire context.Patch.Chunks array has to be looked up in every iteration
                        if (context.Patch is null || !Array.Exists(context.Patch.Chunks, c => c.TargetGid == targetChunk.Gid))
                        {
                            chunks.Add(targetChunk);
                            chunksSize += targetChunk.UncompressedSize;
                            context.DownloadSize += targetChunk.CompressedSize;
                        }
                        chunkOffset++;
                        j--;
                    }
                    else if (sourceChunk.Offset != targetChunk.Offset) //A chunk got relocated (changed its position within the file)
                    {
                        relocEntries.Add(new()
                        {
                            OldOffset = sourceChunk.Offset,
                            NewOffset = targetChunk.Offset,
                            Size = sourceChunk.UncompressedSize
                        });
                        context.InstallSize += sourceChunk.UncompressedSize;
                    }
                }
                for (j += chunkOffset; j < targetFile.Chunks.Length; j++) //Add remaining chunks that are unique to the target file
                {
                    var chunk = targetFile.Chunks[j];
                    if (context.Patch is null || !Array.Exists(context.Patch.Chunks, c => c.TargetGid == chunk.Gid))
                    {
                        chunks.Add(chunk);
                        chunksSize += chunk.UncompressedSize;
                        context.DownloadSize += chunk.CompressedSize;
                    }
                }
                if (chunks.Count > 0)
                {
                    deltas.Add(new()
                    {
                        Name = targetFile.Name,
                        Incomplete = chunks.Count != targetFile.Chunks.Length,
                        FileSize = targetFile.Size,
                        ChunksSize = chunksSize,
                        Chunks = chunks.ToArray()
                    });
                    context.InstallSize += chunksSize;
                    chunks.Clear();
                }
                if (relocEntries.Count > 0)
                {
                    relocs.Add(new()
                    {
                        FileName = targetFile.Name,
                        Entries = relocEntries.ToArray()
                    });
                    relocEntries.Clear();
                }
            }
        }
        for (int j = i; j < context.SourceManifest.Files.Length; j++) //Check remaining files that are unique to the source manifest
            removed.Add(context.SourceManifest.Files[j].Name);
        for (int j = i + offset; j < context.TargetManifest!.Files.Length; j++) //Add remaining files that are unique to the target manifest
        {
            var file = context.TargetManifest.Files[j];
            deltas.Add(new()
            {
                Name = file.Name,
                Incomplete = false,
                FileSize = file.Size,
                ChunksSize = file.Size,
                Chunks = file.Chunks
            });
            foreach (var chunk in file.Chunks)
                context.DownloadSize += chunk.CompressedSize;
            context.InstallSize += file.Size;
        }
        if (removed.Count > 0)
        {
            context.RemovedFiles = removed.ToArray();
            removed.Clear();
        }
        if (relocs.Count > 0)
        {
            context.Relocations = relocs.ToArray();
            relocs.Clear();
        }
        if (deltas.Count == 0)
            return null;
        var result = deltas.ToArray();
        deltas.Clear();
        return result;
    }
    /// <summary>Runs specified tasks for a Steam item.</summary>
    /// <param name="depotId">ID of the depot to perform tasks for.</param>
    /// <param name="tasks">Flags specifying tasks that should be performed.</param>
    /// <param name="eventHandlers">Handlers for client events.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <param name="modDetails">When <paramref name="depotId"/> specified a workshop depot, details of the mod to run tasks for.</param>
    public static void RunTasks(uint depotId, Tasks tasks, EventHandlers eventHandlers, CancellationToken cancellationToken, in Mod.ModDetails modDetails = default)
    {
        var context = new TaskContext() { Item = depotId == 346110 ? new(modDetails.Id) : new(depotId) };
        if ((tasks & Tasks.GetUpdateData) != 0)
        {
            eventHandlers.SetStage?.Invoke(LocCode.CheckingForUpdates, false);
            bool itemInstalled = depotId switch
            {
                346110 => Directory.Exists($@"{Mod.CompressedModsDirectory}\{context.Item.ModId}"),
                346111 => Directory.Exists($@"{Game.Path}\ShooterGame") && Directory.EnumerateFileSystemEntries($@"{Game.Path}\ShooterGame").GetEnumerator().MoveNext(),
                _ => Array.Find(DLC.List, d => d.DepotId == depotId)?.IsInstalled ?? false
            };
            if (itemInstalled && CurrentManifestIds.TryGetValue(context.Item, out context.SourceManifestId))
            {
                if (depotId == 346110 || Environment.TickCount64 - ManifestIdsLastUpdated > 600000)
                {
                    eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.GettingLatestManifestID), 0);
                    if (depotId == 346110)
                    {
                        context.TargetManifestId = modDetails.ManifestId;
                        if (context.TargetManifestId == 0)
                            context.TargetManifestId = CM.Client.GetModManifestId(context.Item.ModId);
                    }
                    else
                        CM.Client.UpdateDepotManifestIds();
                }
                if (depotId != 346110)
                    context.TargetManifestId = DepotManifestIds[depotId];
                if (context.SourceManifestId == context.TargetManifestId)
                {
                    string itemName = depotId switch
                    {
                        346110 => modDetails.Name,
                        346111 => "ARK",
                        _ => Array.Find(DLC.List, d => d.DepotId == depotId)!.Name
                    };
                    eventHandlers.SetStatus?.Invoke(string.Format(LocManager.GetString(LocCode.AlreadyUpToDate), itemName), 1);
                    return;
                }
                eventHandlers.SetStage?.Invoke(LocCode.GettingSourceManifest, true);
                context.SourceManifest = CDNClient.GetManifest(context.Item, context.SourceManifestId, eventHandlers, cancellationToken);
                eventHandlers.SetStage?.Invoke(LocCode.GettingTargetManifest, true);
                context.TargetManifest = CDNClient.GetManifest(context.Item, context.TargetManifestId, eventHandlers, cancellationToken);
                eventHandlers.SetStage?.Invoke(LocCode.GettingPatch, true);
                context.Patch = CDNClient.GetPatch(context.Item, context.SourceManifestId, context.TargetManifestId, eventHandlers, cancellationToken);
                eventHandlers.SetStage?.Invoke(LocCode.ComputingChanges, true);
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.ComputingChanges), 0);
                context.Deltas = ComputeDeltas(ref context);
            }
            else
                tasks |= Tasks.Validate;
        }
        if ((tasks & Tasks.Validate) != 0)
        {
            eventHandlers.SetStage?.Invoke(LocCode.Validating, false);
            if (depotId == 346110 || Environment.TickCount64 - ManifestIdsLastUpdated > 600000)
            {
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.GettingLatestManifestID), 0);
                if (depotId == 346110)
                {
                    context.TargetManifestId = modDetails.ManifestId;
                    if (context.TargetManifestId == 0)
                        context.TargetManifestId = CM.Client.GetModManifestId(context.Item.ModId);
                }
                else
                    CM.Client.UpdateDepotManifestIds();
            }
            if (depotId != 346110)
                context.TargetManifestId = DepotManifestIds[depotId];
            context.TargetManifest = CDNClient.GetManifest(context.Item, context.TargetManifestId, eventHandlers, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();
            bool itemInstalled = depotId switch
            {
                346110 => Directory.Exists($@"{Mod.CompressedModsDirectory}\{context.Item.ModId}"),
                346111 => Directory.Exists($@"{Game.Path}\ShooterGame") && Directory.EnumerateFileSystemEntries($@"{Game.Path}\ShooterGame").GetEnumerator().MoveNext(),
                _ => Array.Find(DLC.List, d => d.DepotId == depotId)?.IsInstalled ?? false
            };
            if (itemInstalled)
            {
                int validationStartIndex = 0;
                var deltas = new List<DeltaFile>();
                string validationCachePatch = $@"{DownloadsFolder}\{context.Item}-{context.TargetManifestId}.vcache";
                Span<byte> buffer = stackalloc byte[20];
                int filesMissing = 0;
                int filesOutdated = 0;
                int filesUpToDate = 0;
                if (File.Exists(validationCachePatch))
                {
                    eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.ReadingValidationCache), 0);
                    using var stream = File.OpenRead(validationCachePatch);
                    if (stream.Read(buffer[..4]) == 4)
                    {
                        if (CRC32.ComputeHash(stream) == BitConverter.ToUInt32(buffer))
                        {
                            stream.Position = 4;
                            stream.Read(buffer);
                            validationStartIndex = BitConverter.ToInt32(buffer);
                            filesMissing = BitConverter.ToInt32(buffer.Slice(4, 4));
                            filesOutdated = BitConverter.ToInt32(buffer.Slice(8, 4));
                            filesUpToDate = BitConverter.ToInt32(buffer.Slice(12, 4));
                            int numDeltas = BitConverter.ToInt32(buffer.Slice(16, 4));
                            deltas.Capacity = numDeltas;
                            Span<byte> nameBuffer = stackalloc byte[260];
                            Span<byte> chunkBuffer = nameBuffer[..40];
                            Span<byte> fileBuffer = chunkBuffer[..13];
                            for (int i = 0; i < numDeltas; i++)
                            {
                                int nameSize = stream.ReadByte();
                                stream.Read(nameBuffer[..nameSize]);
                                string name = Encoding.UTF8.GetString(nameBuffer[..nameSize]);
                                stream.Read(fileBuffer);
                                bool incomplete = fileBuffer[0] != 0;
                                long fileSize = BitConverter.ToInt64(fileBuffer.Slice(1, 8));
                                var chunks = new ChunkEntry[BitConverter.ToInt32(fileBuffer.Slice(9, 4))];
                                long chunksSize = 0;
                                for (int j = 0; j < chunks.Length; j++)
                                {
                                    stream.Read(chunkBuffer);
                                    var gid = new Hash(chunkBuffer[..20].ToArray());
                                    uint chunkHash = BitConverter.ToUInt32(chunkBuffer.Slice(20, 4));
                                    long offset = BitConverter.ToInt64(chunkBuffer.Slice(24, 8));
                                    int compressedSize = BitConverter.ToInt32(chunkBuffer.Slice(32, 4));
                                    int uncompressedSize = BitConverter.ToInt32(chunkBuffer.Slice(36, 4));
                                    chunksSize += uncompressedSize;
                                    chunks[j] = new()
                                    {
                                        Gid = gid,
                                        Hash = chunkHash,
                                        Offset = offset,
                                        CompressedSize = compressedSize,
                                        UncompressedSize = uncompressedSize
                                    };
                                }
                                deltas.Add(new()
                                {
                                    Name = name,
                                    Incomplete = incomplete,
                                    FileSize = fileSize,
                                    ChunksSize = chunksSize,
                                    Chunks = chunks
                                });
                            }
                        }
                        else
                        {
                            stream.Close();
                            File.Delete(validationCachePatch);
                        }
                    }
                    else
                    {
                        stream.Close();
                        File.Delete(validationCachePatch);
                    }
                }
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException();
                if (validationStartIndex < context.TargetManifest.Files.Length)
                {
                    string statusPath = $@"{DownloadsFolder}\{context.Item}-{context.TargetManifestId}.status";
                    if (File.Exists(statusPath))
                        File.Delete(statusPath);
                    long totalSize = 0;
                    for (int i = validationStartIndex; i < context.TargetManifest.Files.Length; i++)
                        totalSize += context.TargetManifest.Files[i].Size;
                    var validationContext = new ValidationContext(filesMissing, filesOutdated, filesUpToDate, validationStartIndex, cancellationToken);
                    long lastProgress = 0;
                    eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.Validating), 0);
                    eventHandlers.PrepareProgress?.Invoke(true, totalSize);
                    eventHandlers.StartValidation?.Invoke();
                    using var timer = new Timer(delegate
                    {
                        long newProgress = validationContext.Progress;
                        eventHandlers.UpdateProgress?.Invoke(newProgress - lastProgress);
                        eventHandlers.UpdateCounters?.Invoke(validationContext.FilesMissing, validationContext.FilesOutdated, validationContext.FilesUpToDate);
                        lastProgress = newProgress;
                    }, null, 0, 200);
                    Validate(context.Item, context.TargetManifest.Files, deltas, ref validationContext);
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                    eventHandlers.UpdateProgress?.Invoke(validationContext.Progress - lastProgress);
                    eventHandlers.UpdateCounters?.Invoke(validationContext.FilesMissing, validationContext.FilesOutdated, validationContext.FilesUpToDate);
                    if (!cancellationToken.IsCancellationRequested && deltas.Count == 0)
                    {
                        string itemName = depotId switch
                        {
                            346110 => modDetails.Name,
                            346111 => "ARK",
                            _ => Array.Find(DLC.List, d => d.DepotId == depotId)!.Name
                        };
                        eventHandlers.SetStatus?.Invoke(string.Format(LocManager.GetString(LocCode.AlreadyUpToDate), itemName), 1);
                        CurrentManifestIds[context.Item] = context.TargetManifestId;
                        if (File.Exists(statusPath))
                            File.Delete(statusPath);
                        if (File.Exists(validationCachePatch))
                            File.Delete(validationCachePatch);
                        return;
                    }
                    using var stream = File.Create(validationCachePatch);
                    stream.Position = 4;
                    BitConverter.TryWriteBytes(buffer[..4], validationContext.Index);
                    BitConverter.TryWriteBytes(buffer.Slice(4, 4), validationContext.FilesMissing);
                    BitConverter.TryWriteBytes(buffer.Slice(8, 4), validationContext.FilesOutdated);
                    BitConverter.TryWriteBytes(buffer.Slice(12, 4), validationContext.FilesUpToDate);
                    BitConverter.TryWriteBytes(buffer.Slice(16, 4), deltas.Count);
                    stream.Write(buffer);
                    Span<byte> nameBuffer = stackalloc byte[260];
                    Span<byte> chunkBuffer = nameBuffer[..40];
                    Span<byte> fileBuffer = chunkBuffer[..13];
                    foreach (var delta in deltas)
                    {
                        int nameSize = Encoding.UTF8.GetBytes(delta.Name, nameBuffer);
                        stream.WriteByte((byte)nameSize);
                        stream.Write(nameBuffer[..nameSize]);
                        fileBuffer[0] = (byte)(delta.Incomplete ? 1 : 0);
                        BitConverter.TryWriteBytes(fileBuffer.Slice(1, 8), delta.FileSize);
                        BitConverter.TryWriteBytes(fileBuffer.Slice(9, 4), delta.Chunks.Length);
                        stream.Write(fileBuffer);
                        foreach (var chunk in delta.Chunks)
                        {
                            chunk.Gid.Data.Span.CopyTo(chunkBuffer[..20]);
                            BitConverter.TryWriteBytes(chunkBuffer.Slice(20, 4), chunk.Hash);
                            BitConverter.TryWriteBytes(chunkBuffer.Slice(24, 8), chunk.Offset);
                            BitConverter.TryWriteBytes(chunkBuffer.Slice(32, 4), chunk.CompressedSize);
                            BitConverter.TryWriteBytes(chunkBuffer.Slice(36, 4), chunk.UncompressedSize);
                            stream.Write(chunkBuffer);
                        }
                    }
                    stream.Position = 4;
                    uint hash = CRC32.ComputeHash(stream);
                    BitConverter.TryWriteBytes(buffer[..4], hash);
                    stream.Position = 0;
                    stream.Write(buffer[..4]);
                }
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException();
                context.Deltas = deltas.ToArray();
                foreach (var delta in context.Deltas)
                {
                    foreach (var chunk in delta.Chunks)
                        context.DownloadSize += chunk.CompressedSize;
                    context.InstallSize += delta.ChunksSize;
                }
            }
            else
            {
                context.Deltas = new DeltaFile[context.TargetManifest.Files.Length];
                for (int i = 0; i < context.Deltas.Length; i++)
                {
                    var file = context.TargetManifest.Files[i];
                    context.Deltas[i] = new()
                    {
                        Name = file.Name,
                        Incomplete = false,
                        FileSize = file.Size,
                        ChunksSize = file.Size,
                        Chunks = file.Chunks
                    };
                    foreach (var chunk in file.Chunks)
                        context.DownloadSize += chunk.CompressedSize;
                    context.InstallSize += file.Size;
                }
            }
        }
        if ((tasks & Tasks.ReserveDiskSpace) != 0 && context.Deltas is not null)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();
            if (!File.Exists($@"{DownloadsFolder}\{context.Item}-{context.TargetManifestId}.status"))
            {
                eventHandlers.SetStage?.Invoke(LocCode.ReservingDiskSpace, false);
                string basePath = $@"{DownloadsFolder}\{context.Item}\";
                foreach (var delta in context.Deltas)
                    if (basePath.Length + delta.Name.Length > 255)
                        throw new SteamException(LocManager.GetString(LocCode.PathTooLong));
                long diskFreeSpace = WinAPI.GetDiskFreeSpace(DownloadsFolder) + 20971520; //Add 20 MB to take NTFS entries into account
                if (diskFreeSpace < context.InstallSize)
                    throw new SteamException(string.Format(LocManager.GetString(LocCode.NotEnoughSpace), LocManager.BytesToString(context.InstallSize - diskFreeSpace)));
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.ReservingDiskSpace), 0);
                eventHandlers.PrepareProgress?.Invoke(false, context.Deltas.Length);
                foreach (var delta in context.Deltas)
                {
                    string path = string.Concat(basePath, delta.Name);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using var stream = File.OpenWrite(path);
                    stream.SetLength(delta.ChunksSize);
                    eventHandlers.UpdateProgress?.Invoke(1);
                }
            }
        }
        if ((tasks & Tasks.Download) != 0 && context.Deltas is not null)
        {
            var downloadContext = new DownloadContext(context.Item, context.Deltas, cancellationToken);
            string statusPath = $@"{DownloadsFolder}\{context.Item}-{context.TargetManifestId}.status";
            if (File.Exists(statusPath))
            {
                Span<byte> buffer = stackalloc byte[8];
                using var stream = File.OpenRead(statusPath);
                stream.Read(buffer);
                downloadContext.DeltaIndex = BitConverter.ToInt32(buffer);
                downloadContext.ChunkIndex = BitConverter.ToInt32(buffer[4..]);
            }
            if (downloadContext.DeltaIndex < context.Deltas.Length)
            {
                long downloadedSize = 0;
                for (int i = 0; i < downloadContext.DeltaIndex; i++)
                    foreach (var chunk in context.Deltas[i].Chunks)
                        downloadedSize += chunk.CompressedSize;
                for (int i = 0; i < downloadContext.ChunkIndex; i++)
                    downloadedSize += context.Deltas[downloadContext.DeltaIndex].Chunks[i].CompressedSize;
                CDNClient.CheckServerList();
                eventHandlers.SetStage?.Invoke(LocCode.DownloadingFiles, false);
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.DownloadingFiles), 0);
                eventHandlers.PrepareProgress?.Invoke(true, context.DownloadSize - downloadedSize);
                var threads = new Thread[NumberOfDownloadThreads];
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i] = new(Download, 6291456);
                    threads[i].Start((downloadContext, i));
                }
                long lastProgress = 0;
                using var timer = new Timer(delegate
                {
                    long newProgress = downloadContext.Progress;
                    eventHandlers.UpdateProgress?.Invoke(newProgress - lastProgress);
                    lastProgress = newProgress;
                }, null, 0, 200);
                foreach (var thread in threads)
                    thread.Join();
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                eventHandlers.UpdateProgress?.Invoke(downloadContext.Progress - lastProgress);
                {
                    Span<byte> buffer = stackalloc byte[8];
                    BitConverter.TryWriteBytes(buffer[..4], downloadContext.DeltaIndex);
                    BitConverter.TryWriteBytes(buffer[4..], downloadContext.ChunkIndex);
                    using var stream = File.Create(statusPath);
                    stream.Write(buffer);
                }
                if (downloadContext.Exception is not null)
                {
                    if (downloadContext.Exception is SteamException e)
                        throw new SteamException(e.Message);
                    else
                        throw new AggregateException(downloadContext.Exception);
                }
            }
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();
        }
        if ((tasks & Tasks.Install) != 0)
        {
            eventHandlers.SetStage?.Invoke(LocCode.Installing, false);
            string basePath = $@"{DownloadsFolder}\{context.Item}\";
            string baseLocalPath = string.Concat(depotId == 346110 ? $@"{Mod.CompressedModsDirectory}\{context.Item.ModId}" : Game.Path, "\\");
            Span<byte> buffer = stackalloc byte[1048576];
            FileStream? relocCacheStream = null;
            FileStream? patchCacheStream = null;
            Dictionary<PatchChunkEntry, PatchRecord>? patchRecords = null;
            if (context.Relocations is not null)
            {
                eventHandlers.SetStage?.Invoke(LocCode.ReadingRelocs, true);
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.ReadingRelocs), 0);
                long totalSize = 0;
                foreach (var reloc in context.Relocations)
                    totalSize += reloc.Entries.Length;
                eventHandlers.PrepareProgress?.Invoke(false, totalSize);
                relocCacheStream = new FileStream($@"{DownloadsFolder}\{context.Item}-{context.TargetManifestId}.reloc-cache", FileMode.Create, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete, 1048576, FileOptions.DeleteOnClose);
                foreach (var reloc in context.Relocations)
                {
                    string path = string.Concat(baseLocalPath, reloc.FileName);
                    if (!File.Exists(path))
                        throw new SteamException(LocManager.GetString(LocCode.InstallationCorrupted));
                    using var fileStream = File.OpenRead(path);
                    foreach (var entry in reloc.Entries)
                    {
                        if (entry.OldOffset + entry.Size > fileStream.Length)
                            throw new SteamException(LocManager.GetString(LocCode.InstallationCorrupted));
                        fileStream.Position = entry.OldOffset;
                        fileStream.Read(buffer[..entry.Size]);
                        relocCacheStream.Write(buffer[..entry.Size]);
                        eventHandlers.UpdateProgress?.Invoke(1);
                    }
                }
            }
            if (context.Patch is not null)
            {
                eventHandlers.SetStage?.Invoke(LocCode.ComputingPatchedChunks, true);
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.ComputingPatchedChunks), 0);
                eventHandlers.PrepareProgress?.Invoke(false, context.Patch.Chunks.Length);
                patchRecords = new(context.Patch.Chunks.Length);
                foreach (var patchChunk in context.Patch.Chunks)
                {
                    foreach (var file in context.SourceManifest!.Files)
                    {
                        bool breakLoop = false;
                        foreach (var chunk in file.Chunks)
                            if (chunk.Gid == patchChunk.SourceGid)
                            {
                                var targetFile = Array.Find(context.TargetManifest!.Files, f => f.Name == file.Name);
                                patchRecords.Add(patchChunk, new()
                                {
                                    FileSize = targetFile.Size,
                                    FilePath = string.Concat(baseLocalPath, file.Name),
                                    SourceChunk = chunk,
                                    TargetChunk = Array.Find(targetFile.Chunks, ch => ch.Gid == patchChunk.TargetGid)
                                });
                                breakLoop = true;
                                break;
                            }
                        if (breakLoop)
                            break;
                    }
                    eventHandlers.UpdateProgress?.Invoke(1);
                }
                eventHandlers.SetStage?.Invoke(LocCode.ReadingPatchChunks, true);
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.ReadingPatchChunks), 0);
                eventHandlers.PrepareProgress?.Invoke(false, context.Patch.Chunks.Length);
                var lzmaDecoder = new Utils.LZMA.Decoder();
                Span<byte> targetChunkBuffer = stackalloc byte[1048576];
                patchCacheStream = new FileStream($@"{DownloadsFolder}\{context.Item}-{context.TargetManifestId}.patch-cache", FileMode.Create, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete, 1048576, FileOptions.DeleteOnClose);
                foreach (var pair in patchRecords)
                {
                    using var fileStream = File.OpenRead(pair.Value.FilePath);
                    if (pair.Value.SourceChunk.Offset + pair.Value.SourceChunk.UncompressedSize > fileStream.Length)
                        throw new SteamException(LocManager.GetString(LocCode.InstallationCorrupted));
                    fileStream.Position = pair.Value.SourceChunk.Offset;
                    fileStream.Read(buffer[..pair.Value.SourceChunk.UncompressedSize]);
                    bool decodeSuccess;
                    try { decodeSuccess = lzmaDecoder.Decode(pair.Key.Data.Span, targetChunkBuffer[..pair.Value.TargetChunk.UncompressedSize], buffer[..pair.Value.SourceChunk.UncompressedSize]); }
                    catch (IndexOutOfRangeException) { decodeSuccess = false; }
                    if (!decodeSuccess)
                        throw new SteamException(LocManager.GetString(LocCode.InstallationCorrupted));
                    patchCacheStream.Write(targetChunkBuffer[..pair.Value.TargetChunk.UncompressedSize]);
                    eventHandlers.UpdateProgress?.Invoke(1);
                }
            }
            string fileToDelete = $@"{DownloadsFolder}\{context.Item}-{context.TargetManifestId}.status";
            if (File.Exists(fileToDelete))
                File.Delete(fileToDelete);
            if (context.Relocations is not null)
            {
                eventHandlers.SetStage?.Invoke(LocCode.WritingRelocs, true);
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.WritingRelocs), 0);
                long totalSize = 0;
                foreach (var reloc in context.Relocations)
                    totalSize += reloc.Entries.Length;
                eventHandlers.PrepareProgress?.Invoke(false, totalSize);
                relocCacheStream!.Position = 0;
                foreach (var reloc in context.Relocations)
                {
                    using var fileStream = File.OpenWrite(string.Concat(baseLocalPath, reloc.FileName));
                    foreach (var entry in reloc.Entries)
                    {
                        relocCacheStream.Read(buffer[..entry.Size]);
                        fileStream.Position = entry.NewOffset;
                        fileStream.Write(buffer[..entry.Size]);
                        eventHandlers.UpdateProgress?.Invoke(1);
                    }
                }
            }
            relocCacheStream?.Dispose();
            if (context.Patch is not null)
            {
                eventHandlers.SetStage?.Invoke(LocCode.WritingPatchChunks, true);
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.WritingPatchChunks), 0);
                eventHandlers.PrepareProgress?.Invoke(false, context.Patch.Chunks.Length);
                patchCacheStream!.Position = 0;
                foreach (var pair in patchRecords!)
                {
                    patchCacheStream.Read(buffer[..pair.Value.TargetChunk.UncompressedSize]);
                    using var fileStream = File.OpenWrite(pair.Value.FilePath);
                    fileStream.Position = pair.Value.TargetChunk.Offset;
                    fileStream.Write(buffer[..pair.Value.TargetChunk.UncompressedSize]);
                    fileStream.SetLength(pair.Value.FileSize);
                    eventHandlers.UpdateProgress?.Invoke(1);
                }
            }
            if (context.Deltas is not null)
            {
                eventHandlers.SetStage?.Invoke(LocCode.InstallingFiles, true);
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.InstallingFiles), 0);
                long totalSize = 0;
                foreach (var delta in context.Deltas)
                    totalSize += delta.ChunksSize;
                eventHandlers.PrepareProgress?.Invoke(true, totalSize);
                foreach (var delta in context.Deltas)
                {
                    string path = string.Concat(basePath, delta.Name);
                    string localPath = string.Concat(baseLocalPath, delta.Name);
                    if (delta.Incomplete)
                    {
                        if (!File.Exists(localPath))
                            throw new SteamException(LocManager.GetString(LocCode.InstallationCorrupted));
                        using var stream = File.OpenRead(path);
                        using var localStream = File.OpenWrite(localPath);
                        localStream.SetLength(delta.FileSize);
                        foreach (var chunk in delta.Chunks)
                        {
                            stream.Read(buffer[..chunk.UncompressedSize]);
                            localStream.Position = chunk.Offset;
                            localStream.Write(buffer[..chunk.UncompressedSize]);
                            eventHandlers.UpdateProgress?.Invoke(chunk.UncompressedSize);
                        }
                        stream.Close();
                        File.Delete(path);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                        File.Move(path, localPath, true);
                        eventHandlers.UpdateProgress?.Invoke(delta.FileSize);
                    }
                }
            }
            if (context.RemovedFiles is not null)
            {
                eventHandlers.SetStage?.Invoke(LocCode.RemovingFiles, true);
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.RemovingFiles), 0);
                eventHandlers.PrepareProgress?.Invoke(false, context.RemovedFiles.Length);
                foreach (string file in context.RemovedFiles)
                {
                    string path = string.Concat(baseLocalPath, file);
                    if (File.Exists(path))
                        File.Delete(path);
                    eventHandlers.UpdateProgress?.Invoke(1);
                }
            }
            fileToDelete = $@"{DownloadsFolder}\{context.Item}-{context.TargetManifestId}.vcache";
            if (File.Exists(fileToDelete))
                File.Delete(fileToDelete);
            Directory.Delete(basePath, true);
            if (depotId == 346110)
            {
                string infoFilePath = string.Concat(baseLocalPath, "mod.info");
                if (File.Exists(infoFilePath))
                    File.SetLastWriteTimeUtc(infoFilePath, DateTime.UtcNow);
            }
            else
            {
                CurrentManifestIds[context.Item] = context.TargetManifestId;
                eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.UpdateFinished), 1);
            }
        }
        if ((tasks & Tasks.UnpackMod) != 0)
        {
            eventHandlers.SetStage?.Invoke(LocCode.UnpackingModFiles, false);
            eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.UnpackingModFiles), 0);
            string basePath = $@"{Mod.CompressedModsDirectory}\{context.Item.ModId}";
            Span<byte> buffer = stackalloc byte[4];
            string[] names;
            using (var stream = File.OpenRead($@"{basePath}\mod.info"))
            {
                ReadString(stream);
                stream.Read(buffer);
                names = new string[BitConverter.ToInt32(buffer)];
                for (int i = 0; i < names.Length; i++)
                    names[i] = ReadString(stream);
            }
            var metas = new Dictionary<string, string>();
            using (var stream = File.OpenRead($@"{basePath}\modmeta.info"))
            {
                stream.Read(buffer);
                int numMetas = BitConverter.ToInt32(buffer);
                for (int i = 0; i < numMetas; i++)
                    metas[ReadString(stream)] = ReadString(stream);
            }
            string modFolderPath = $@"{Game.Path}\ShooterGame\Content\Mods\{context.Item.ModId}";
            if (Directory.Exists(modFolderPath))
                Directory.Delete(modFolderPath, true);
            string modFilePath = string.Concat(modFolderPath, ".mod");
            if (File.Exists(modFilePath))
                File.Delete(modFilePath);
            string compressedFilesPath = string.Concat(basePath, @"\WindowsNoEditor");
            var compressedFiles = Directory.EnumerateFiles(compressedFilesPath, "*", SearchOption.AllDirectories);
            int numFiles = 0;
            for (var enumerator = compressedFiles.GetEnumerator(); enumerator.MoveNext(); numFiles++);
            eventHandlers.PrepareProgress?.Invoke(false, numFiles);
            var inflater = new Inflater();
            byte[] compressedChunkBuffer = Array.Empty<byte>();
            byte[] uncompressedChunkBuffer = Array.Empty<byte>();
            foreach (string sourceFile in compressedFiles)
            {
                string destinationFile = sourceFile.Replace(compressedFilesPath, modFolderPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                if (sourceFile.EndsWith(".z"))
                {
                    using var reader = File.OpenRead(sourceFile);
                    using var writer = File.Create(destinationFile[..^2]);
                    long size = new ChunkMeta(reader).UncompressedSize;
                    if (size == 1641380927)
                        size = 131072;
                    else if (size == 0)
                        size = 1;
                    int numChunks = (int)((new ChunkMeta(reader).UncompressedSize + size - 1) / size);
                    var chunkMetas = new ChunkMeta[numChunks];
                    for (int i = 0; i < numChunks; i++)
                        chunkMetas[i] = new ChunkMeta(reader);
                    long maxCompressedSize = 0;
                    long maxUncompressedSize = 0;
                    foreach (var chunkMeta in chunkMetas)
                    {
                        if (chunkMeta.CompressedSize > maxCompressedSize)
                            maxCompressedSize = chunkMeta.CompressedSize;
                        if (chunkMeta.UncompressedSize > maxUncompressedSize)
                            maxUncompressedSize = chunkMeta.UncompressedSize;
                    }
                    if (maxCompressedSize > compressedChunkBuffer.LongLength)
                        compressedChunkBuffer = new byte[maxCompressedSize];
                    if (maxUncompressedSize > uncompressedChunkBuffer.LongLength)
                        uncompressedChunkBuffer = new byte[maxUncompressedSize];
                    foreach (var chunkSize in chunkMetas)
                    {
                        int compressedSize = (int)chunkSize.CompressedSize;
                        int uncompressedSize = (int)chunkSize.UncompressedSize;
                        inflater.Reset();
                        reader.Read(compressedChunkBuffer, 0, compressedSize);
                        inflater.SetInput(compressedChunkBuffer, 0, compressedSize);
                        inflater.Inflate(uncompressedChunkBuffer, 0, uncompressedSize);
                        writer.Write(uncompressedChunkBuffer, 0, uncompressedSize);
                    }
                }
                else
                    File.Copy(sourceFile, destinationFile, true);
                eventHandlers.UpdateProgress?.Invoke(1);
            }
            using (var stream = File.Create(modFilePath))
            {
                buffer = stackalloc byte[8];
                BitConverter.TryWriteBytes(buffer, context.Item.ModId);
                stream.Write(buffer);
                WriteString(stream, modDetails.Status == 1 ? modDetails.Name : "ModName");
                WriteString(stream, $"../../../ShooterGame/Content/Mods/{context.Item.ModId}");
                BitConverter.TryWriteBytes(buffer[..4], names.Length);
                stream.Write(buffer[..4]);
                foreach (string name in names)
                    WriteString(stream, name);
                BitConverter.TryWriteBytes(buffer[..4], 4280483635);
                BitConverter.TryWriteBytes(buffer[4..], 2);
                stream.Write(buffer);
                stream.WriteByte((byte)(metas.ContainsKey("ModType") ? int.Parse(metas["ModType"]) : 0));
                BitConverter.TryWriteBytes(buffer[..4], metas.Count);
                stream.Write(buffer[..4]);
                foreach (var meta in metas)
                {
                    WriteString(stream, meta.Key);
                    WriteString(stream, meta.Value);
                }
            }
        }
        if ((tasks & Tasks.FinishModInstall) != 0)
        {
            lock (Mod.List)
            {
                ulong id = context.Item.ModId;
                if (Mod.List.Find(m => m.Id == id) is null)
                    Mod.List.Add(new(id) { Details = modDetails });
                Application.Current.Dispatcher.InvokeAsync(delegate
                {
                    var mainWindow = (MainWindow)Application.Current.MainWindow;
                    if (mainWindow.TabFrame.Child is ModsTab modsTab)
                        modsTab.ReloadList();
                    else if (mainWindow.TabFrame.Child is ClusterTab clusterTab)
                        foreach (ServerItem item in clusterTab.Servers.Children)
                        {
                            var server = (Server)item.DataContext;
                            if (Array.IndexOf(server.ModIds, id) >= 0)
                                foreach (ServerModItem modItem in item.Mods.Children)
                                    if (modItem.Id == id)
                                    {
                                        modItem.SetInstalled();
                                        break;
                                    }
                        }
                });
            }
            CurrentManifestIds[context.Item] = context.TargetManifestId;
            eventHandlers.SetStatus?.Invoke(string.Format(LocManager.GetString(LocCode.ModInstallSuccess), modDetails.Name), 1);
        }
    }
    /// <summary>Represents a meta block of Zlib-compressed data chunk.</summary>
    readonly record struct ChunkMeta
    {
        /// <summary>Compressed size of the chunk in bytes.</summary>
        public readonly long CompressedSize;
        /// <summary>Uncompressed size of the chunk in bytes.</summary>
        public readonly long UncompressedSize;
        /// <summary>Initializes a new chunk meta object by reading it from specified file stream.</summary>
        /// <param name="stream">File stream to read the meta block from.</param>
        public ChunkMeta(FileStream stream)
        {
            Span<byte> buffer = stackalloc byte[16];
            stream.Read(buffer);
            CompressedSize = BitConverter.ToInt64(buffer);
            UncompressedSize = BitConverter.ToInt64(buffer[8..]);
        }
    }
    /// <summary>Provides information about the file and source and target chunks for patching.</summary>
    readonly record struct PatchRecord
    {
        /// <summary>Gets size of the file after patching.</summary>
        public readonly long FileSize { get; init; }
        /// <summary>Gets path to the file that contains the chunk to patch.</summary>
        public readonly string FilePath { get; init; }
        /// <summary>Gets the entry for chunk before patching.</summary>
        public readonly ChunkEntry SourceChunk { get; init; }
        /// <summary>Gets the entry for chunk after patching.</summary>
        public readonly ChunkEntry TargetChunk { get; init; }
    }
    /// <summary>Shared context for tasks executed by client for a single item.</summary>
    ref struct TaskContext
    {
        /// <summary>Total size of all files scheduled to download in bytes.</summary>
        public long DownloadSize;
        /// <summary>Total size of all files scheduled to install in bytes.</summary>
        public long InstallSize;
        /// <summary>ID of the source manifest (usually manifest of local installation).</summary>
        public ulong SourceManifestId;
        /// <summary>ID of the target manifest.</summary>
        public ulong TargetManifestId;
        /// <summary>When not <see langword="null"/>, list of the files that were removed in target version.</summary>
        public string[]? RemovedFiles;
        /// <summary>When not <see langword="null"/>, list of delta files that were added/updated in target version.</summary>
        public DeltaFile[]? Deltas;
        /// <summary>When not <see langword="null"/>, source depot manifest object.</summary>
        public DepotManifest? SourceManifest;
        /// <summary>When not <see langword="null"/>, target depot manifest object.</summary>
        public DepotManifest? TargetManifest;
        /// <summary>When not <see langword="null"/>, depot patch object that contains patch data for updating from <see cref="SourceManifest"/> to <see cref="TargetManifest"/>.</summary>
        public DepotPatch? Patch;
        /// <summary>Identifier of the item to perform tasks for.</summary>
        public ItemIdentifier Item;
        /// <summary>When not <see langword="null"/>, list of chunk relocations in target version grouped by files.</summary>
        public Relocation[]? Relocations;
    }
    /// <summary>Shared context for validation procedure and progress update timer.</summary>
    struct ValidationContext
    {
        /// <summary>Initializes a new validation context with specified start index and file counters.</summary>
        /// <param name="filesMissing">Number of previously found missing files.</param>
        /// <param name="filesOutdated">Number of previously found outdated files.</param>
        /// <param name="filesUpToDate">Number of previously found up-to-date files.</param>
        /// <param name="index">Index of the file to start/continue validation at.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        public ValidationContext(int filesMissing, int filesOutdated, int filesUpToDate, int index, CancellationToken cancellationToken)
        {
            FilesMissing = filesMissing;
            FilesOutdated = filesOutdated;
            FilesUpToDate = filesUpToDate;
            Index = index;
            CancellationToken = cancellationToken;
        }
        /// <summary>Number of files that are determined as missing.</summary>
        public int FilesMissing;
        /// <summary>Number of files that are determined as outdated.</summary>
        public int FilesOutdated;
        /// <summary>Number of files that are determined as up to date.</summary>
        public int FilesUpToDate;
        /// <summary>Index of the next file to validate.</summary>
        public int Index;
        /// <summary>Current validation progress in bytes.</summary>
        public long Progress = 0;
        /// <summary>Token to monitor for cancellation requests.</summary>
        public readonly CancellationToken CancellationToken;
    }
    /// <summary>Represents a shared context for all download threads.</summary>
    class DownloadContext
    {
        /// <summary>Initializes a new download context for specified item and deltas.</summary>
        /// <param name="item">Identifier of the item to download.</param>
        /// <param name="deltas">Array of delta files that have to be downloaded.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        public DownloadContext(ItemIdentifier item, DeltaFile[] deltas, CancellationToken cancellationToken)
        {
            Item = item;
            CancellationToken = cancellationToken;
            Deltas = deltas;
        }
        /// <summary>Index of the chunk within delta to assign to next available download thread.</summary>
        public int ChunkIndex;
        /// <summary>Index of the delta within <see cref="Deltas"/> to assign to next available download thread.</summary>
        public int DeltaIndex;
        /// <summary>Current download progress in bytes.</summary>
        public long Progress;
        /// <summary>Exception that occurred in one of download threads, <see langword="null"/> if no exceptions were thrown.</summary>
        public Exception? Exception;
        /// <summary>Identifier of the item to download.</summary>
        public readonly ItemIdentifier Item;
        /// <summary>Token to monitor for cancellation requests.</summary>
        public readonly CancellationToken CancellationToken;
        /// <summary>Array of delta files that have to be downloaded.</summary>
        public readonly DeltaFile[] Deltas;
    }
}