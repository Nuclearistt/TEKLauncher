using System.IO;
using System.Security.Cryptography;
using static System.Array;

namespace TEKLauncher.SteamInterop.Network.CM
{
    internal class EncryptionFilter
    {
        internal EncryptionFilter(byte[] SessionKey) => Copy(this.SessionKey = SessionKey, HMAC = new byte[16], 16);
        private readonly byte[] HMAC, SessionKey;
        internal byte[] Decrypt(byte[] Data)
        {
            byte[] DecryptedData, HMACHash, IV;
            using (Aes AES = Aes.Create())
            {
                AES.BlockSize = 128;
                AES.KeySize = 256;
                AES.Mode = CipherMode.ECB;
                AES.Padding = PaddingMode.None;
                IV = new byte[16];
                Copy(Data, IV, 16);
                byte[] Cipher = new byte[Data.Length - 16];
                Copy(Data, 16, Cipher, 0, Cipher.Length);
                using (ICryptoTransform Transform = AES.CreateDecryptor(SessionKey, null))
                    IV = Transform.TransformFinalBlock(IV, 0, 16);
                AES.Mode = CipherMode.CBC;
                AES.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform Transform = AES.CreateDecryptor(SessionKey, IV))
                using (MemoryStream Stream = new MemoryStream(Cipher))
                using (CryptoStream CryptroStream = new CryptoStream(Stream, Transform, CryptoStreamMode.Read))
                {
                    DecryptedData = new byte[Cipher.Length];
                    Resize(ref DecryptedData, CryptroStream.Read(DecryptedData, 0, DecryptedData.Length));
                }
            }
            using (HMACSHA1 HMACSHA = new HMACSHA1(HMAC))
            using (MemoryStream Stream = new MemoryStream())
            {
                Stream.Write(IV, 13, 3);
                Stream.Write(DecryptedData, 0, DecryptedData.Length);
                Stream.Position = 0L;
                HMACHash = HMACSHA.ComputeHash(Stream);
            }
            for (int Iterator = 0; Iterator < 13; Iterator++)
                if (HMACHash[Iterator] != IV[Iterator])
                    throw new IOException();
            return DecryptedData;
        }
        internal byte[] Encrypt(byte[] Data)
        {
            byte[] IV = new byte[16];
            using (RandomNumberGenerator RNG = RandomNumberGenerator.Create())
                RNG.GetBytes(IV, 13, 3);
            using (HMACSHA1 HMACSHA = new HMACSHA1(HMAC))
            using (MemoryStream Stream = new MemoryStream())
            {
                Stream.Write(IV, 13, 3);
                Stream.Write(Data, 0, Data.Length);
                Stream.Position = 0L;
                byte[] Hash = HMACSHA.ComputeHash(Stream);
                Copy(Hash, IV, 13);
            }
            using (Aes AES = Aes.Create())
            {
                AES.BlockSize = 128;
                AES.KeySize = 256;
                AES.Mode = CipherMode.ECB;
                AES.Padding = PaddingMode.None;
                byte[] EncryptedIV;
                using (ICryptoTransform Transform = AES.CreateEncryptor(SessionKey, null))
                    EncryptedIV = Transform.TransformFinalBlock(IV, 0, 16);
                AES.Mode = CipherMode.CBC;
                AES.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform Transform = AES.CreateEncryptor(SessionKey, IV))
                using (MemoryStream Stream = new MemoryStream())
                using (CryptoStream CryptoStream = new CryptoStream(Stream, Transform, CryptoStreamMode.Write))
                {
                    CryptoStream.Write(Data, 0, Data.Length);
                    CryptoStream.FlushFinalBlock();
                    byte[] Cipher = Stream.ToArray(), Output = new byte[16 + Cipher.Length];
                    Copy(EncryptedIV, Output, 16);
                    Copy(Cipher, 0, Output, 16, Cipher.Length);
                    return Output;
                }
            }
        }
    }
}