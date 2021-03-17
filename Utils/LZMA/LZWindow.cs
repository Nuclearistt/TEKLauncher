using System.IO;

namespace TEKLauncher.Utils.LZMA
{
    internal class LZWindow
    {
        private byte[] Buffer;
        private uint Position, StreamPosition, WindowSize;
        private Stream Stream;
        internal void CopyBlock(uint Offset, uint Length)
        {
            uint Position = this.Position - Offset - 1U;
            if (Position >= WindowSize)
                Position += WindowSize;
            for (; Length > 0U; Length--)
            {
                if (Position >= WindowSize)
                    Position = 0;
                Buffer[this.Position++] = Buffer[Position++];
                if (this.Position >= WindowSize)
                    Flush();
            }
        }
        internal void Create(uint WindowSize)
        {
            if (this.WindowSize != WindowSize)
                Buffer = new byte[this.WindowSize = WindowSize];
            StreamPosition = Position = 0U;
        }
        internal void Flush()
        {
            uint Size = Position - StreamPosition;
            if (Size != 0U)
            {
                Stream.Write(Buffer, (int)StreamPosition, (int)Size);
                if (Position >= WindowSize)
                    Position = 0U;
                StreamPosition = Position;
            }
        }
        internal void Initialize(Stream Stream)
        {
            StreamPosition = Position = 0U;
            this.Stream = Stream;
        }
        internal void PutByte(byte Byte)
        {
            Buffer[Position++] = Byte;
            if (Position >= WindowSize)
                Flush();
        }
        internal byte GetByte(uint Offset)
        {
            uint Position = this.Position - Offset - 1U;
            if (Position >= WindowSize)
                Position += WindowSize;
            return Buffer[Position];
        }
    }
}