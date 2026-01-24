using System.Security.Cryptography;
using System.Text;
using Aizen.Core.Common.Abstraction.Exception;

namespace Aizen.Core.Security;

public enum AizenHashType
{
    Sha256,
    Sha384,
    Sha512
}

public static class AizenHash
{
    public static string ComputeHash(AizenHashType hashType, string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new AizenException($"Input cannot be null or empty.");
        }

        var hashProvider = AizenHashProviderFactory.CreateHash(hashType);

        return hashProvider.ComputeHash(input.EncodeString()).ToHexValue();
    }

    private static byte[] EncodeString(this string source)
    {
        return Encoding.UTF8.GetBytes(source);
    }
}

internal static class AizenHashProviderFactory
{
    public static HashAlgorithm CreateHash(AizenHashType hashType)
    {
        return hashType switch
        {
            AizenHashType.Sha256 => new SHA256Managed(),
            AizenHashType.Sha384 => new SHA384Managed(),
            AizenHashType.Sha512 => new SHA512Managed(),
            _ => new SHA256Managed(),
        };
    }

    public static HMAC CreateHashWithSalt(AizenHashType hashType, byte[] key)
    {
        return hashType switch
        {
            AizenHashType.Sha256 => new HMACSHA256(key),
            AizenHashType.Sha384 => new HMACSHA384(key),
            AizenHashType.Sha512 => new HMACSHA512(key),
            _ => new HMACSHA256(key),
        };
    }
}