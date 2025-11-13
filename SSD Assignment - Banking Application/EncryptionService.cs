using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SSD_Assignment___Banking_Application
{
    public static class EncryptionService
    {
        public static string EncryptString(string plainText, byte[] key)
        {
            byte[] plainBytes = Encoding.ASCII.GetBytes(plainText);

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                byte[] cipherBytes;

                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();
                    cipherBytes = ms.ToArray();
                }

                byte[] ivAndCipher = new byte[aes.IV.Length + cipherBytes.Length];
                Buffer.BlockCopy(aes.IV, 0, ivAndCipher, 0, aes.IV.Length);
                Buffer.BlockCopy(cipherBytes, 0, ivAndCipher, aes.IV.Length, cipherBytes.Length);

                byte[] hmac;
                using (var hmacSha = new HMACSHA256(key))
                {
           
                    hmac = hmacSha.ComputeHash(ivAndCipher);
                }
                byte[] fullData = new byte[aes.IV.Length + cipherBytes.Length + hmac.Length];
                Buffer.BlockCopy(aes.IV, 0, fullData, 0, aes.IV.Length);
                Buffer.BlockCopy(cipherBytes, 0, fullData, aes.IV.Length, cipherBytes.Length);
                Buffer.BlockCopy(hmac, 0, fullData, aes.IV.Length + cipherBytes.Length, hmac.Length);

                return Convert.ToBase64String(fullData);
            }
        }

        public static string DecryptString(string base64Cipher, byte[] key)
        {
            byte[] FullcipherBytes = Convert.FromBase64String(base64Cipher);

            byte[] iv = new byte[16];
            byte[] hmac = new byte[32];
            byte[] cipherBytes = new byte[FullcipherBytes.Length - iv.Length - hmac.Length];
            Buffer.BlockCopy(FullcipherBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(FullcipherBytes, iv.Length, cipherBytes, 0, cipherBytes.Length);
            Buffer.BlockCopy(FullcipherBytes, iv.Length + cipherBytes.Length, hmac, 0, hmac.Length);

            // Validate HMAC
            byte[] ivAndCipher = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, ivAndCipher, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, ivAndCipher, iv.Length, cipherBytes.Length);

            using (var hmacSha = new HMACSHA256(key))
            {
                byte[] expectedHmac = hmacSha.ComputeHash(ivAndCipher);
                if (!CryptographicOperations.FixedTimeEquals(expectedHmac, hmac))
                    throw new CryptographicException("HMAC validation failed.");
            }

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.IV = iv;

                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherBytes, 0, cipherBytes.Length);
                    cs.FlushFinalBlock();
                    byte[] plainBytes = ms.ToArray();
                    return Encoding.ASCII.GetString(plainBytes);
                }
            }
        }
    }
}
