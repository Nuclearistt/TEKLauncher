using System;
using System.Collections.Generic;
using System.IO;
using TEKLauncher.Utils;

namespace TEKLauncher.SteamInterop.Network.Manifest
{
    internal class Payload : ProtoObject
    {
        internal List<FileEntry> Files = new List<FileEntry>();
        private static readonly Comparison<ChunkEntry> Comparator = (A, B) => A.Offset.CompareTo(B.Offset);
        protected private override int[] Indexes => new[] { 1 };
        protected private override void ReadField(int Index, Stream Stream)
        {
            FileEntry File = ReadProtoObject<FileEntry>(Stream);
            if ((File.Flags & 64) == 0)
            {
                File.Chunks.Sort(Comparator);
                Files.Add(File);
            }
        }
        internal override void Serialize(MemoryStream Stream) { }
    }
}