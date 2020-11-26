using System.Security.Cryptography;
using static System.BitConverter;

namespace TEKLauncher.Utils
{
    internal class CRC32 : HashAlgorithm
    {
        static CRC32()
        {
            for (uint Iterator = 0U; Iterator < 256U; Iterator++)
            {
                uint Entry = Iterator;
                for (int Iterator1 = 0; Iterator1 < 8; Iterator1++)
                    if ((Entry & 1U) == 1U)
                        Entry = (Entry >> 1) ^ 0xEDB88320U;
                    else
                        Entry >>= 1;
                Table[Iterator] = Entry;
            }
        }
        internal CRC32() => CRCHash = 0xFFFFFFFFU;
        private uint CRCHash;
        internal static readonly uint[] Table = new uint[256];
        public override int HashSize => 32;
        protected override void HashCore(byte[] Buffer, int Offset, int Length)
        {
            for (int Iterator = Offset; Iterator < Length; Iterator++)
                CRCHash = unchecked((CRCHash >> 8) ^ Table[Buffer[Iterator] ^ CRCHash & 0xFFU]);
        }
        protected override byte[] HashFinal() => GetBytes(~CRCHash);
        public override void Initialize() => CRCHash = 0xFFFFFFFFU;
    }
}