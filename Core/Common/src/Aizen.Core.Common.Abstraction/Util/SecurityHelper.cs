using System.Security.Cryptography;
using System.Text;

namespace Aizen.Core.Common.Abstraction.Util;

public class SecurityHelper
{
    /// <summary>
    ///  Encrypt AES
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public static string EncryptAES<T>(T t, string saltKey) where T: class
    {
        var serializedData = t.Serialize();

        var key = Encoding.ASCII.GetBytes(saltKey);
        var plainText = serializedData;
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
}