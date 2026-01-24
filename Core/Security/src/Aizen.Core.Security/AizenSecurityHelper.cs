using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Aizen.Core.Security
{
    public static class AizenSecurityHelper
    {
        private const string passphrase = "Sup3rS3curePass!";

        public static string ToBinary(this int Decimal)
        {
            Int64 BinaryHolder;
            char[] BinaryArray;
            string BinaryResult = "";

            while (Decimal > 0)
            {
                BinaryHolder = Decimal % 2;
                BinaryResult += BinaryHolder;
                Decimal = Decimal / 2;
            }

            BinaryArray = BinaryResult.ToCharArray();
            Array.Reverse(BinaryArray);
            BinaryResult = new string(BinaryArray);

            return BinaryResult.Reverse();
        }
        public static string Reverse(this string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        public static string Encrypt(this string text)
        {
            return AizenStringEncryption.OpenSSLEncrypt(text, "19F65C17EF-B3C9-4474-13BOM17-B3BF-E404FC4164C705");
        }

        public static string Decrypt(this string encrypt)
        {
            return AizenStringEncryption.OpenSSLDecrypt(encrypt, "19F65C17EF-B3C9-4474-13BOM17-B3BF-E404FC4164C705");
        }

        public static string EncryptByAES(this string text, string key)
        {
            return AizenStringEncryption.EncryptDataByAES(text, key);
        }

        public static string DecryptByAES(this string encryptText, string saltKey)
        {
            return AizenStringEncryption.DecryptDataByAES(encryptText, saltKey);
        }


        private static byte[] IV =
     {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16
        };

        private static byte[] DeriveKeyFromPassword(string password)
        {
            var emptySalt = Array.Empty<byte>();
            var iterations = 1000;
            var desiredKeyLength = 16; // 16 bytes equal 128 bits.
            var hashMethod = HashAlgorithmName.SHA384;
            return Rfc2898DeriveBytes.Pbkdf2(Encoding.Unicode.GetBytes(password),
                                             emptySalt,
                                             iterations,
                                             hashMethod,
                                             desiredKeyLength);
        }


        public static string CustomDecryptAsync(this string cipherText)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(passphrase);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                        {
                            return streamReader.ReadToEnd().Base64Decode();
                        }
                    }
                }
            }
        }
        public static string CustomEncryptAsync(this string plainText)
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(passphrase);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(plainText.Base64Encode());
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(this string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }


        public static string EncryptString(string key, string plainText)
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }



        public static string CustomDecryptAsynct(string key, string cipherText)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }



        public static Dictionary<string, string> FormatHeaders(IHeaderDictionary headers)
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                DateTime selectedDate = DateTime.Now;
                if (DateTime.TryParse(header.Value, out selectedDate))
                {
                    var info = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
                    DateTimeOffset localServerTime = selectedDate;
                    DateTimeOffset localTime = TimeZoneInfo.ConvertTime(localServerTime, info);
                    pairs.Add(header.Key, localTime.DateTime.ToString("ddd, dd MMM yyy HH:mm:ss �GMT�"));
                }
                else
                {
                    pairs.Add(header.Key, header.Value);
                }
            }
            return pairs;
        }


        public static Dictionary<string, string> FormatHeaders(HttpContentHeaders headers)
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                pairs.Add(header.Key, String.Join(',', header.Value));
            }
            return pairs;
        }


        public static List<KeyValuePair<string, string>> FormatQueries(string queryString)
        {
            List<KeyValuePair<string, string>> pairs =
                 new List<KeyValuePair<string, string>>();
            string key, value;
            foreach (var query in queryString.TrimStart('?').Split("&"))
            {
                var items = query.Split("=");
                key = items.Count() >= 1 ? items[0] : string.Empty;
                value = items.Count() >= 2 ? items[1] : string.Empty;
                if (!String.IsNullOrEmpty(key))
                {
                    pairs.Add(new KeyValuePair<string, string>(key, value));
                }
            }
            return pairs;
        }

        public static async Task<string> ReadBodyFromRequest(HttpRequest request)
        {
            request.EnableBuffering();
            using var streamReader = new StreamReader(request.Body, leaveOpen: true);
            var requestBody = await streamReader.ReadToEndAsync();
            request.Body.Position = 0;
            return requestBody;
        }

        public static Dictionary<string, string> FormatHeaders(HttpRequestHeaders headers)
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                pairs.Add(header.Key, String.Join(',', header.Value));
            }
            return pairs;
        }

        public static DateTime GetTurkeyLocalization(DateTime? date)
        {
            var info = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            DateTimeOffset localServerTime = date.HasValue ? date.Value : DateTimeOffset.Now;
            DateTimeOffset localTime = TimeZoneInfo.ConvertTime(localServerTime, info);
            return localTime.DateTime;
        }
    }
}