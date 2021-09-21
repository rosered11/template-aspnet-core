using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace template.domain.Utilities
{
    public static class RsaCipher
    {
        public static string DecryptToBase64(string base64, X509Certificate2 certificate)
        {
            var rsaPrivate = certificate.GetRSAPrivateKey();
            var bytes = Convert.FromBase64String(base64);
            var decryptByte = rsaPrivate.Decrypt(bytes, RSAEncryptionPadding.Pkcs1);
            //return Encoding.UTF8.GetString(decryptByte);
            return Convert.ToBase64String(decryptByte);
        }

        public static string EncryptToBase64(string data, X509Certificate2 certificate)
        {
            var rsaPublic = certificate.GetRSAPublicKey();
            var bytes = Convert.FromBase64String(data);
            var encryptByte = rsaPublic.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
            //return Encoding.UTF8.GetString(encryptByte);
            return Convert.ToBase64String(encryptByte);
        }

        public static string Decrypt(string base64, X509Certificate2 certificate)
        {
            var rsaPrivate = certificate.GetRSAPrivateKey();
            var bytes = Convert.FromBase64String(base64);
            var decryptByte = rsaPrivate.Decrypt(bytes, RSAEncryptionPadding.Pkcs1);
            return Encoding.UTF8.GetString(decryptByte);
        }

        public static string Encrypt(string data, X509Certificate2 certificate)
        {
            var rsaPublic = certificate.GetRSAPublicKey();
            var bytes = Convert.FromBase64String(data);
            var encryptByte = rsaPublic.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
            return Encoding.UTF8.GetString(encryptByte);
        }
    }

    public static class AesCipher
    {
        private const int KeySieze = 256;
        
        private const int DerivationIterations = 1000;

        public static string Encrypt(string plainText, string passPhrase, out byte[] saltStringBytes, out byte[] ivStringBytes)
        {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.
            saltStringBytes = Generate256BitsOfRandomEntropy();
            ivStringBytes = Generate256BitsOfRandomEntropy();
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using(var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(KeySieze / 8);
                using(var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 128;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using(var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using(var memoryStream = new MemoryStream())
                        {
                            using(var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                // Create the final bytes as a concatemation of the random salt bytes, the random iv bytes and the cipher bytes.
                                var cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                //memoryStream.Close();
                                //cryptoStream.Close();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public static string Decrypt(string cipherText, string passPhrase, string saltText, string ivText)
        {
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
            var cipherTextBytesWithSaltAndIV = Convert.FromBase64String(cipherText);
            var salt = Convert.FromBase64String(saltText);
            var iv = Convert.FromBase64String(ivText);

            /* === Get the salt bytes by extracting the first 32 bytes from the supplied cipher text bytes. === */
            // cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
            var saltStringBytes = salt;

            /* === Get the IV bytes by extracting the next 32 bytes from the supplied cipher text bytes. === */
            // cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
            var ivStringBytes = iv;

            /* === Get the actual cipher text bytes by removing the first 64 bites from the cipher text string. */
            // cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();
            var cipherTextBytes = cipherTextBytesWithSaltAndIV;

            using(var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(KeySieze / 8);
                using(var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using(var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                var plainTextBytes = new byte[cipherTextBytes.Length];
                                var descryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                //memoryStream.Close();
                                //cryptoStream.Close();
                                return Convert.ToBase64String(plainTextBytes);
                            }
                        }
                    }
                }
            }
        }
        private static byte[] Generate256BitsOfRandomEntropy()
        {
            // 32 Bytes will give us 256 bits.
            var randomBytes = new byte[16];
            using(var rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with cryptographically secure random bytes
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }
    }
}