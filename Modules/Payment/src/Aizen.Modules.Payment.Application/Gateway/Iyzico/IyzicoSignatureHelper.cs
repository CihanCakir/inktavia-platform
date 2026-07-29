using System.Security.Cryptography;
using System.Text;

namespace Aizen.Modules.Payment.Application.Gateway.Iyzico;

/// <summary>
/// BE-P9-fix — iyzico IYZWSv2 request signing (§1), HPP webhook V3 signature (§2), and response signature validation (§3),
/// per the official docs (docs.iyzico.com, 2026-07-28). Pure + static so the byte-exact HMAC vectors are unit-provable
/// without any live keys.
/// </summary>
public static class IyzicoSignatureHelper
{
    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    private static string HmacHex(string key, string data)
        => Hex(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(data)));

    // ── §1 IYZWSv2 request signing ──────────────────────────────────────────────

    /// <summary>encryptedData = HEX_lower(HMACSHA256(secretKey, randomKey + uriPath + requestBody)).</summary>
    public static string ComputeRequestSignature(string secretKey, string uriPath, string randomKey, string requestBody)
        => HmacHex(secretKey, randomKey + uriPath + requestBody);

    /// <summary>
    /// The full <c>Authorization</c> header value:
    /// <c>"IYZWSv2 " + base64("apiKey:"+apiKey+"&amp;randomKey:"+randomKey+"&amp;signature:"+encryptedData)</c>.
    /// </summary>
    public static string BuildAuthorizationHeader(string apiKey, string secretKey, string uriPath, string randomKey, string requestBody)
    {
        var signature  = ComputeRequestSignature(secretKey, uriPath, randomKey, requestBody);
        var authString = "apiKey:" + apiKey + "&randomKey:" + randomKey + "&signature:" + signature;
        return "IYZWSv2 " + Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
    }

    // ── §2 HPP webhook V3 signature ─────────────────────────────────────────────

    /// <summary>
    /// HPP (CheckoutForm) webhook signature (§2):
    /// <c>HEX_lower(HMACSHA256(secretKey, secretKey + iyziEventType + iyziPaymentId + token + paymentConversationId + status))</c>.
    /// </summary>
    public static string ComputeHppWebhookSignatureV3(
        string secretKey, string iyziEventType, string iyziPaymentId, string token, string paymentConversationId, string status)
        => HmacHex(secretKey, secretKey + iyziEventType + iyziPaymentId + token + paymentConversationId + status);

    public static bool ValidateHppWebhookSignatureV3(
        string secretKey, string iyziEventType, string? iyziPaymentId, string token, string? paymentConversationId, string status, string headerSignature)
        => string.Equals(
            ComputeHppWebhookSignatureV3(secretKey, iyziEventType, iyziPaymentId ?? "", token, paymentConversationId ?? "", status),
            headerSignature, StringComparison.OrdinalIgnoreCase);

    // ── §3 response signature validation ────────────────────────────────────────

    /// <summary>Trailing-zero trim for price params: "10.50"→"10.5", "10.0"→"10", "10"→"10".</summary>
    public static string TrimPrice(string? price)
    {
        if (string.IsNullOrEmpty(price)) return price ?? "";
        if (!price.Contains('.')) return price;
        var trimmed = price.TrimEnd('0').TrimEnd('.');
        return trimmed.Length == 0 ? "0" : trimmed;
    }

    /// <summary>Response signature = HEX_lower(HMACSHA256(secretKey, params joined by ':')). Caller supplies ordered params (prices pre-trimmed).</summary>
    public static string ComputeResponseSignature(string secretKey, params string[] orderedParams)
        => HmacHex(secretKey, string.Join(":", orderedParams));

    public static bool ValidateResponseSignature(string secretKey, string? headerOrBodySignature, params string[] orderedParams)
        => !string.IsNullOrEmpty(headerOrBodySignature)
           && string.Equals(ComputeResponseSignature(secretKey, orderedParams), headerOrBodySignature, StringComparison.OrdinalIgnoreCase);
}
