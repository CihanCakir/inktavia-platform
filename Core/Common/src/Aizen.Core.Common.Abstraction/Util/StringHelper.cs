using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Aizen.Core.Common.Abstraction.Enums;


namespace Aizen.Core.Common.Abstraction.Util
{
    public static class StringHelper
    {
        private static readonly Random _random = new();
        private static readonly string _alphanumeric = "0123456789";
        private static readonly string _upperCaseCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly string _lowerCaseCharacters = "abcdefghijklmnopqrstuvwxyz";

        public static string Generate(int length, AizenStringCasing casing = default, bool isAlphanumeric = false, bool isOnlyNumeric = false)
        {
            var charset = casing switch
            {
                AizenStringCasing.Lower => _lowerCaseCharacters,
                AizenStringCasing.Upper => _upperCaseCharacters,
                AizenStringCasing.Mixed => _lowerCaseCharacters + _upperCaseCharacters,
                _ => _lowerCaseCharacters
            };


            if (isAlphanumeric)
            {
                charset += _alphanumeric;
            }

            if (isOnlyNumeric)
            {
                charset = _alphanumeric;
            }

            return Generate(length, charset);
        }

        public static string Generate(int length, string charset)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(charset);

            return new string(Enumerable.Repeat(stringBuilder.ToString(), length)
                            .Select(s => s[_random.Next(s.Length)])
                            .ToArray());
        }
        
        public static string ConvertUnicodeToTurkish(string input)
        {
            string result = Regex.Replace(input, @"\\u([0-9A-Fa-f]{4})", match => ((char)Int32.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber)).ToString());

            return result;
        }
        
                public static string MaskEmail(this string value, bool isVisible)
        {
            if (isVisible)
            {
                return value;
            }

            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (!value.Contains("@")) return new String('*', value.Length);
            if (value.Split('@')[0].Length < 4) return @"*@*.*";
            return Regex.Replace(value, @"(?<=[\w]{1})[\w-\._\+%]*(?=[\w]{1}@)", m => new string('*', m.Length));
        }

        public static string MaskPhone(this string value, bool isVisible)
        {
            if (isVisible)
            {
                return value;
            }

            return string.IsNullOrEmpty(value) || value.Length != 10
                ? string.Empty
                : "+90(" + value.Substring(0, 3) + ")" + new string('*', value.Length - 6) +
                  value.Substring(value.Length - 3, 3);
        }

        public static string MaskCardNo(this string value)
        {
            value = value.Length > 16
                ? value[..16]
                : value;
            return value.Substring(0, 3) + new string('*', value.Length - 10) + value.Substring(value.Length - 3, 3);
        }

        public static string ConvertToEnglishCharacters(this string input)
        {
            // Türkçe karakterleri İngilizce karakterlere dönüştüren bir StringBuilder oluştur
            StringBuilder result = new StringBuilder();

            foreach (char c in input)
            {
                switch (c)
                {
                    case 'ç':
                        result.Append("c");
                        break;
                    case 'ğ':
                        result.Append("g");
                        break;
                    case 'ı':
                        result.Append("i");
                        break;
                    case 'ö':
                        result.Append("o");
                        break;
                    case 'ş':
                        result.Append("s");
                        break;
                    case 'ü':
                        result.Append("u");
                        break;
                    case 'Ç':
                        result.Append("C");
                        break;
                    case 'Ğ':
                        result.Append("G");
                        break;
                    case 'İ':
                        result.Append("I");
                        break;
                    case 'Ö':
                        result.Append("O");
                        break;
                    case 'Ş':
                        result.Append("S");
                        break;
                    case 'Ü':
                        result.Append("U");
                        break;
                    default:
                        result.Append(c);
                        break;
                }
            }

            return result.ToString();
        }

        public static string MaskTCKN(this string value, bool isVisible)
        {
            if (isVisible)
            {
                return value;
            }

            return string.IsNullOrEmpty(value) || value == "11111111111" || value.Length != 11
                ? string.Empty
                : value.Substring(0, 3) + new String('*', value.Length - 3) + value.Substring(value.Length - 3, 3);
        }

        public static string? MaskName(this string value)
        {
            if (!string.IsNullOrEmpty(value) || value != "")
            {
                if (value.Length <= 2)
                    return value;
                else
                    return value.Substring(0, 2) + new String('*', value.Length - 2);
                ;
            }

            return null;
        }
        public static string? MaskFirstWord(this string value)
        {
            if (!string.IsNullOrEmpty(value) || value != "")
            {
                string firstWord = value.Split(' ')[0];

                string firstTwoLetters = firstWord.Substring(0, Math.Min(2, firstWord.Length));

                return firstTwoLetters + "***";
            }

            return null;
        }
    }
}
