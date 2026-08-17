using System.Security.Cryptography;
using System.Text;

namespace Aizen.Modules.Identity.Repository.Identity.Service.PasswordRecovery;

/// <summary>
/// Cryptographic helpers for password recovery: OTP generation, opaque token generation, salted PBKDF2 hashing
/// with constant-time verification, and target masking. No raw OTP/token is ever persisted or logged.
/// </summary>
public static class PasswordRecoverySecurity
{
    private const int Pbkdf2Iterations = 100_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public static string GenerateNumericOtp(int length)
    {
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
            sb.Append((char)('0' + RandomNumberGenerator.GetInt32(0, 10)));
        return sb.ToString();
    }

    /// <summary>URL-safe opaque token (used for resetRequestId and the reset token).</summary>
    public static string GenerateOpaqueToken(int bytes = 32)
    {
        var buffer = RandomNumberGenerator.GetBytes(bytes);
        return Convert.ToBase64String(buffer).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public static (string hash, string salt) Hash(string value)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(value), salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, HashBytes);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verify(string value, string expectedHashBase64, string saltBase64)
    {
        if (string.IsNullOrEmpty(expectedHashBase64) || string.IsNullOrEmpty(saltBase64)) return false;
        var salt = Convert.FromBase64String(saltBase64);
        var expected = Convert.FromBase64String(expectedHashBase64);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(value), salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var name = email[..at];
        var domain = email[(at + 1)..];
        var maskedName = name.Length <= 1 ? name : $"{name[0]}{new string('*', Math.Min(name.Length - 1, 3))}";
        return $"{maskedName}@{domain}";
    }

    public static string MaskPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length < 4) return "••••";
        var last2 = digits[^2..];
        return $"{(phone.StartsWith('+') ? "+" : string.Empty)}{new string('•', Math.Max(digits.Length - 2, 2))} {last2}";
    }
}
