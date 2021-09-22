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

        public static string Encrypt(string cipherBase64, X509Certificate2 certificate)
        {
            var rsaPublic = certificate.GetRSAPublicKey();
            var bytes = Convert.FromBase64String(cipherBase64);
            var encryptByte = rsaPublic.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
            //return Encoding.UTF8.GetString(encryptByte);
            return Convert.ToBase64String(encryptByte);
        }
    }

    public static class RijndaelCipher
    {
        // This constant determines the number of iterations for the password bytes generation function.
        private const int RcfIterations = 1000;
        // This consant is used to determine the key size of the encryption algorithm in bits.
        // We divide the by 8 within the code below to get the equivalent number of bytes.
        private const int KeySize = 256;
        public static string Encrypt(string dataText, byte[] key, byte[] iv)
        {
            string encryptedBase64 = null;
            using (RijndaelManaged myRijndael = new RijndaelManaged())
            {
                // Encrypt the string to an array of bytes.
                using (var encryptor = myRijndael.CreateEncryptor(key, iv))
                {
                    using(MemoryStream msEncrypt = new MemoryStream())
                    {
                        using(CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using(StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(dataText);
                            }
                            encryptedBase64 = Convert.ToBase64String(msEncrypt.ToArray());
                        }
                    }
                }
            }
            return encryptedBase64;
        }

        public static string EncryptWithPassword(string dataText, string password, string saltBase64, string ivBase64)
        {
            string encryptedBase64 = null;
            var salt = Convert.FromBase64String(saltBase64);
            var iv = Convert.FromBase64String(ivBase64);

            using (RijndaelManaged myRijndael = new RijndaelManaged())
            {
                myRijndael.BlockSize = 128;
                myRijndael.Mode = CipherMode.CBC;
                myRijndael.Padding = PaddingMode.PKCS7;

                // Implements password-based key derivation functionality, PBKDF2, by using a pseudo-random number generator based on HMACSHA1.
                // https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes?view=net-5.0
                using(var rfc = new Rfc2898DeriveBytes(password, salt, RcfIterations))
                {
                    var key = rfc.GetBytes(KeySize / 8);
                    // Encrypt the string to an array of bytes.
                    using (var encryptor = myRijndael.CreateEncryptor(key, iv))
                    {
                        using(MemoryStream msEncrypt = new MemoryStream())
                        {
                            using(CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                            {
                                using(StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                                {
                                    swEncrypt.Write(dataText);
                                }
                                encryptedBase64 = Convert.ToBase64String(msEncrypt.ToArray());
                            }
                        }
                    }
                }
            }
            return encryptedBase64;
        }

        public static string DecryptWithPassword(string cipherText, string password, string saltBase64, string ivBase64)
        {
            string plaintext = null;

            byte[] cipherByte = Convert.FromBase64String(cipherText);
            var salt = Convert.FromBase64String(saltBase64);
            var iv = Convert.FromBase64String(ivBase64);

            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.BlockSize = 128;
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.PKCS7;

                // Implements password-based key derivation functionality, PBKDF2, by using a pseudo-random number generator based on HMACSHA1.
                // https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes?view=net-5.0
                using(var rfc = new Rfc2898DeriveBytes(password, salt, RcfIterations))
                {
                    var key = rfc.GetBytes(KeySize / 8);
                    // Create a decryptor to perform the stream transform.
                    using(ICryptoTransform decryptor = rijAlg.CreateDecryptor(key, iv))
                    {
                        // Create the streams used for decryption.
                        using (MemoryStream msDecrypt = new MemoryStream(cipherByte))
                        {
                            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                            {
                                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                                {
                                    // Read the decrypted bytes from the decrypting stream
                                    // and place them in a string.
                                    plaintext = srDecrypt.ReadToEnd();
                                }
                            }
                        }
                    }
                }
            }

            return plaintext;
        }

        public static string Decrypt(string cipherBase64, byte[] key, byte[] iv)
        {
            string plaintext = null;

            byte[] cipherByte = Convert.FromBase64String(cipherBase64);

            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(key, iv);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherByte))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }
    }

    public static class AesCipher
    {
        private const int KeySieze = 256;
        
        private const int DerivationIterations = 1000;

        #region  Obsolete encrypt and decrypt function
        [Obsolete]
        public static string Encrypt(string plainText, string passPhrase, out byte[] saltStringBytes, out byte[] ivStringBytes)
        {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.
            saltStringBytes = RandomGenerator.Generate256BitsOfRandomEntropy();
            ivStringBytes = RandomGenerator.Generate256BitsOfRandomEntropy();
            Console.WriteLine("plainText ==> " + plainText);
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            plainTextBytes = new System.Text.UTF8Encoding(false).GetBytes(plainText);
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
                                //cryptoStream.FlushFinalBlock();
                                // Create the final bytes as a concatemation of the random salt bytes, the random iv bytes and the cipher bytes.
                                var cipherTextBytes = memoryStream.ToArray();
                                // cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                // cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                //memoryStream.Close();
                                //cryptoStream.Close();
                                Console.WriteLine("Length ==> " + cipherTextBytes.Length);
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        [Obsolete]
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
                                //cryptoStream.FlushFinalBlock();
                                return Convert.ToBase64String(plainTextBytes);
                            }
                        }
                    }
                }
            }
        }

        #endregion
        public static byte[] Encrypt2(string plainText, byte[] key, byte[] iv)
        {
            byte[] encrypted;
            using(Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                using(var encryptor = aesAlg.CreateEncryptor(key, iv))
                {
                    using (var msEncrypt = new MemoryStream())
                    {
                        using( var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using(var swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(plainText);
                            }
                            encrypted = msEncrypt.ToArray();
                        }
                    }
                }
            }
            return encrypted;
        }

        public static string Decrypt2(byte[] cipherText, byte[] key, byte[] iv)
        {
            string plainText = null;

            using(var aesAlg = Aes.Create())
            {
                using(var descriptor = aesAlg.CreateDecryptor(key, iv))
                {
                    using(MemoryStream msDescrypt = new MemoryStream(cipherText))
                    {
                        using(CryptoStream csDescrypt = new CryptoStream(msDescrypt, descriptor, CryptoStreamMode.Read))
                        {
                            using(StreamReader srDecrypt = new StreamReader(csDescrypt))
                            {
                                plainText = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            return plainText;
        }
    }

    public static class RandomGenerator
    {
        public static byte[] Generate256BitsOfRandomEntropy()
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