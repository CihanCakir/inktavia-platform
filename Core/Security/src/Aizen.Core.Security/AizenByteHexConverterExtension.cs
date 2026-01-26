using System.Text;

namespace Aizen.Core.Security;

public static class AizenByteHexConverterExtension
{
    /// <summary>
    /// Converts byte array to hex string.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string ToHexValue(this byte[] input)
    {
        var hex = new StringBuilder(input.Length * 2);
        foreach (var b in input)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString();
    }

    /// <summary>
    /// Converts hex string to  byte array.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static byte[] ToByteArray(this string input)
    {
        var numberChars = input.Length;
        var bytes = new byte[numberChars / 2];
        for (var i = 0; i < numberChars; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(input.Substring(i, 2), 16);
        }

        return bytes;
    }
}