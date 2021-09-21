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

    public static class RijndaelCipher
    {
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
                                cryptoStream.FlushFinalBlock();
                                return Convert.ToBase64String(plainTextBytes);
                            }
                        }
                    }
                }
            }
        }

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

    public static class RandomGenerator
    {
        private const int KeySize = 256;
        //The default iteration count is 1000 so the two methods use the same iteration count.
        private const int DerivationIterations = 500;
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
        
        // Implements password-based key derivation functionality, PBKDF2, by using a pseudo-random number generator based on HMACSHA1.
        // https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes?view=net-5.0
        public static void GenerateKeyFromPassword(string password)
        {
            // Create a byte array to hold the random value.
            var salt = Generate256BitsOfRandomEntropy();

            //data1 can be a string or contents of a file.
            string data1 = "Some test data";
            
            try
            {
                Rfc2898DeriveBytes k1 = new Rfc2898DeriveBytes(password, salt, DerivationIterations);
                Rfc2898DeriveBytes k2 = new Rfc2898DeriveBytes(password, salt, DerivationIterations);
                // Encrypt the data.
                Aes encAlg = Aes.Create();
                encAlg.Key = k1.GetBytes(KeySize / 8);
                MemoryStream encryptionStream = new MemoryStream();
                CryptoStream encrypt = new CryptoStream(encryptionStream,
encAlg.CreateEncryptor(), CryptoStreamMode.Write);
                byte[] utfD1 = new System.Text.UTF8Encoding(false).GetBytes(
data1);

                encrypt.Write(utfD1, 0, utfD1.Length);
                encrypt.FlushFinalBlock();
                encrypt.Close();
                byte[] edata1 = encryptionStream.ToArray();
                k1.Reset();

                // Try to decrypt, thus showing it can be round-tripped.
                Aes decAlg = Aes.Create();
                decAlg.Key = k2.GetBytes(KeySize / 8);
                decAlg.IV = encAlg.IV;
                MemoryStream decryptionStreamBacking = new MemoryStream();
                CryptoStream decrypt = new CryptoStream(
decryptionStreamBacking, decAlg.CreateDecryptor(), CryptoStreamMode.Write);
                decrypt.Write(edata1, 0, edata1.Length);
                decrypt.Flush();
                decrypt.Close();
                k2.Reset();
                string data2 = new UTF8Encoding(false).GetString(
decryptionStreamBacking.ToArray());

                if (!data1.Equals(data2))
                {
                    Console.WriteLine("Error: The two values are not equal.");
                }
                else
                {
                    Console.WriteLine($"The two values are equal. {data2}");
                    Console.WriteLine("k1 iterations: {0}", k1.IterationCount);
                    Console.WriteLine("k2 iterations: {0}", k2.IterationCount);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
        }
    }
}