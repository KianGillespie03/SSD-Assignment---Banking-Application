using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SSD_Assignment___Banking_Application
{
    public class EncryptionService
    {
        public static string EncryptString(string plainText, byte[] key, out byte[] iv, out byte[] hmac)
        {
            byte[] plainBytes = Encoding.ASCII.GetBytes(plainText);

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // generate random IV
                iv = new byte[16];
                RandomNumberGenerator.Fill(iv);
                aes.IV = iv;

                byte[] cipherBytes;

                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();
                    cipherBytes = ms.ToArray();
                }

                
                using (var hmacSha = new HMACSHA256(key))
                {
                    byte[] ivAndCipher = new byte[iv.Length + cipherBytes.Length];
                    Buffer.BlockCopy(iv, 0, ivAndCipher, 0, iv.Length);
                    Buffer.BlockCopy(cipherBytes, 0, ivAndCipher, iv.Length, cipherBytes.Length);
                    hmac = hmacSha.ComputeHash(ivAndCipher);
                }

                // return base64 of ciphertext only; iv and hmac returned separately
                return Convert.ToBase64String(cipherBytes);
            }
        }

        public static string DecryptString(string base64Cipher, byte[] key, byte[] iv, byte[] hmac)
        {
            byte[] cipherBytes = Convert.FromBase64String(base64Cipher);

            // verify HMAC
            using (var hmacSha = new HMACSHA256(key))
            {
                byte[] ivAndCipher = new byte[iv.Length + cipherBytes.Length];
                Buffer.BlockCopy(iv, 0, ivAndCipher, 0, iv.Length);
                Buffer.BlockCopy(cipherBytes, 0, ivAndCipher, iv.Length, cipherBytes.Length);

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
