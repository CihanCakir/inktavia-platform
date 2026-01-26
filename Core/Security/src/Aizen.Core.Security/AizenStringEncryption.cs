using System.Security.Cryptography;
using System.Text;

namespace Aizen.Core.Security
{
    public class AizenStringEncryption
    {
        private const string _unicKey = "@!.Aizen!.";
        public static byte[] Encrypt(string strToBeEncrypted)
        {
            return Encrypt(strToBeEncrypted, null, false);
        }

        public static byte[] Encrypt(string strToBeEncrypted, bool? isStaticSalt = false)
        {
            return Encrypt(strToBeEncrypted, null, false);
        }

        public static byte[] Encrypt(string strToBeEncrypted, string unicKey, bool? isStaticSalt = null)
        {
            if (string.IsNullOrEmpty(unicKey))
                unicKey = _unicKey;
            //Plain Text to be encrypted
            byte[] PlainText = System.Text.Encoding.Unicode.GetBytes(strToBeEncrypted);

            //In live, get the persistant passphrase from other isolated source
            //This example has hardcoded passphrase just for demo purpose
            StringBuilder sb = new StringBuilder();
            sb.Append(unicKey);

            //Generate the Salt, with any custom logic and
            //using the above string
            var Salt = isStaticSalt.GetValueOrDefault(false) ? new byte[16] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76, 0x65, 0x65, 0x65 } : new byte[16];
            if (!isStaticSalt.GetValueOrDefault(false))
                new Random().NextBytes(Salt);

            //Key generation:- default iterations is 1000 
            //and recomended is 10000
            Rfc2898DeriveBytes pwdGen = new Rfc2898DeriveBytes(sb.ToString(), Salt, 1000);

            //The default key size for RijndaelManaged is 256 bits, 
            //while the default blocksize is 128 bits.
            RijndaelManaged _RijndaelManaged = new RijndaelManaged();
            _RijndaelManaged.BlockSize = 128; //Increased it to 256 bits- max and more secure

            byte[] key = pwdGen.GetBytes(_RijndaelManaged.KeySize / 8);   //This will generate a 256 bits key
            byte[] iv = pwdGen.GetBytes(_RijndaelManaged.BlockSize / 8);  //This will generate a 256 bits IV

            //On a given instance of Rfc2898DeriveBytes class,
            //GetBytes() will always return unique byte array.
            _RijndaelManaged.Key = key;
            _RijndaelManaged.IV = iv;

            //Now encrypt
            byte[] cipherText2 = null;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, _RijndaelManaged.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(PlainText, 0, PlainText.Length);
                }
                cipherText2 = ms.ToArray();
            }

            cipherText2 = Salt.Concat(cipherText2).ToArray();

            return cipherText2;
        }


        public static string Decrypt(byte[] cipherText2)
        {
            return Decrypt(cipherText2, null);
        }

        public static string Decrypt(byte[] cipherText2, string unicKey)
        {
            return Encoding.Unicode.GetString(DecryptByte(cipherText2, unicKey));
        }

        public static byte[] DecryptByte(byte[] cipherText2, string unicKey = null)
        {
            if (string.IsNullOrEmpty(unicKey))
                unicKey = _unicKey;
            //In live, get the persistant passphrase from other isolated source
            //This example has hardcoded passphrase just for demo purpose, obtained by 
            //adding current user's firstname + DOB + email
            //You may generate this string with any logic you want.
            StringBuilder sb = new StringBuilder();
            sb.Append(unicKey);

            byte[] Salt = cipherText2.Take(16).ToArray();
            cipherText2 = cipherText2.Skip(16).ToArray();

            //Key generation:- default iterations is 1000 and recomended is 10000
            Rfc2898DeriveBytes pwdGen = new Rfc2898DeriveBytes(sb.ToString(), Salt, 1000);

            //The default key size for RijndaelManaged is 256 bits,
            //while the default blocksize is 128 bits.
            RijndaelManaged _RijndaelManaged = new RijndaelManaged();
            _RijndaelManaged.BlockSize = 128; //Increase it to 256 bits- more secure

            byte[] key = pwdGen.GetBytes(_RijndaelManaged.KeySize / 8);   //This will generate a 256 bits key
            byte[] iv = pwdGen.GetBytes(_RijndaelManaged.BlockSize / 8);  //This will generate a 256 bits IV

            //On a given instance of Rfc2898DeriveBytes class,
            //GetBytes() will always return unique byte array.
            _RijndaelManaged.Key = key;
            _RijndaelManaged.IV = iv;

            //Now decrypt
            byte[] plainText2 = null;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, _RijndaelManaged.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherText2, 0, cipherText2.Length);
                }
                plainText2 = ms.ToArray();
            }

            return plainText2;
        }

        public static string EncryptDataByAES(string text, string saltKey)
        {
            var key = Encoding.ASCII.GetBytes(saltKey);
            var plainText = text;
            byte[] encrypted;

            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.KeySize = 256;
                aesAlg.BlockSize = 128;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;
                aesAlg.IV = new byte[16];
                aesAlg.Key = key;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(encrypted);
        }

        public static string DecryptDataByAES(string text, string saltKey)
        {
            var key = Encoding.ASCII.GetBytes(saltKey);
            string plaintext = null;
            byte[] cipherText = Convert.FromBase64String(text.Replace(' ', '+'));

            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.KeySize = 256;
                aesAlg.BlockSize = 128;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;
                aesAlg.IV = new byte[16];
                aesAlg.Key = key;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }



        //Encryption
        public static string OpenSSLEncrypt(string plainText, string passphrase)
        {
            // generate salt
            byte[] key, iv;
            byte[] salt = new byte[8];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetNonZeroBytes(salt);
            DeriveKeyAndIV(passphrase, salt, out key, out iv);
            // encrypt bytes
            byte[] encryptedBytes = EncryptStringToBytesAes(plainText, key, iv);
            // add salt as first 8 bytes
            byte[] encryptedBytesWithSalt = new byte[salt.Length + encryptedBytes.Length + 8];
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("Salted__"), 0, encryptedBytesWithSalt, 0, 8);
            Buffer.BlockCopy(salt, 0, encryptedBytesWithSalt, 8, salt.Length);
            Buffer.BlockCopy(encryptedBytes, 0, encryptedBytesWithSalt, salt.Length + 8, encryptedBytes.Length);
            // base64 encode
            return Convert.ToBase64String(encryptedBytesWithSalt);
        }

        //Decryption
        public static string OpenSSLDecrypt(string encrypted, string passphrase)
        {
            // base 64 decode
            byte[] encryptedBytesWithSalt = Convert.FromBase64String(encrypted);
            // extract salt (first 8 bytes of encrypted)
            byte[] salt = new byte[8];
            byte[] encryptedBytes = new byte[encryptedBytesWithSalt.Length - salt.Length - 8];
            Buffer.BlockCopy(encryptedBytesWithSalt, 8, salt, 0, salt.Length);
            Buffer.BlockCopy(encryptedBytesWithSalt, salt.Length + 8, encryptedBytes, 0, encryptedBytes.Length);
            // get key and iv
            byte[] key, iv;
            DeriveKeyAndIV(passphrase, salt, out key, out iv);
            return DecryptStringFromBytesAes(encryptedBytes, key, iv);
        }

        private static void DeriveKeyAndIV(string passphrase, byte[] salt, out byte[] key, out byte[] iv)
        {
            // generate key and iv
            List<byte> concatenatedHashes = new List<byte>(48);
            byte[] password = Encoding.UTF8.GetBytes(passphrase);
            byte[] currentHash = new byte[0];
            MD5 md5 = MD5.Create();
            bool enoughBytesForKey = false;
            // See http://www.openssl.org/docs/crypto/EVP_BytesToKey.html#KEY_DERIVATION_ALGORITHM
            while (!enoughBytesForKey)
            {
                int preHashLength = currentHash.Length + password.Length + salt.Length;
                byte[] preHash = new byte[preHashLength];
                Buffer.BlockCopy(currentHash, 0, preHash, 0, currentHash.Length);
                Buffer.BlockCopy(password, 0, preHash, currentHash.Length, password.Length);
                Buffer.BlockCopy(salt, 0, preHash, currentHash.Length + password.Length, salt.Length);
                currentHash = md5.ComputeHash(preHash);
                concatenatedHashes.AddRange(currentHash);
                if (concatenatedHashes.Count >= 48)
                    enoughBytesForKey = true;
            }
            key = new byte[32];
            iv = new byte[16];
            concatenatedHashes.CopyTo(0, key, 0, 32);
            concatenatedHashes.CopyTo(32, iv, 0, 16);
            md5.Clear();
            md5 = null;
        }


        static byte[] EncryptStringToBytesAes(string plainText, byte[] key, byte[] iv)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException("key");
            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException("iv");
            // Declare the stream used to encrypt to an in memory
            // array of bytes.
            MemoryStream msEncrypt;
            // Declare the RijndaelManaged object
            // used to encrypt the data.
            RijndaelManaged aesAlg = null;
            try
            {
                // Create a RijndaelManaged object
                // with the specified key and IV.
                aesAlg = new RijndaelManaged { Mode = CipherMode.CBC, KeySize = 256, BlockSize = 128, Key = key, IV = iv };
                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                // Create the streams used for encryption.
                msEncrypt = new MemoryStream();
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        //Write all data to the stream.
                        swEncrypt.Write(plainText);
                        swEncrypt.Flush();
                        swEncrypt.Close();
                    }
                }
            }
            finally
            {
                // Clear the RijndaelManaged object.
                if (aesAlg != null)
                    aesAlg.Clear();
            }
            // Return the encrypted bytes from the memory stream.
            return msEncrypt.ToArray();
        }

        static string DecryptStringFromBytesAes(byte[] cipherText, byte[] key, byte[] iv)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException("key");
            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException("iv");
            // Declare the RijndaelManaged object
            // used to decrypt the data.
            RijndaelManaged aesAlg = null;
            // Declare the string used to hold
            // the decrypted text.
            string plaintext;
            try
            {
                // Create a RijndaelManaged object
                // with the specified key and IV.
                aesAlg = new RijndaelManaged { Mode = CipherMode.CBC, KeySize = 256, BlockSize = 128, Key = key, IV = iv };
                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                            srDecrypt.Close();
                        }
                    }
                }
            }
            finally
            {
                // Clear the RijndaelManaged object.
                if (aesAlg != null)
                    aesAlg.Clear();
            }
            return plaintext;
        }

    }
}
