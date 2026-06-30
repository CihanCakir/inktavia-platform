using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Payment.Application.Gateway.Iyzico;

/// <summary>
/// Typed HTTP client for the Iyzico REST API.
/// Handles IYZWSv2 HMAC-SHA256 signature generation for every request.
/// All endpoints return null on HTTP error; callers should check response.IsSuccess.
/// </summary>
public sealed class IyzicoHttpClient
{
    private readonly HttpClient _http;
    private readonly IyzicoConfiguration _config;
    private readonly ILogger<IyzicoHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented        = false,
    };

    public IyzicoHttpClient(
        HttpClient http,
        IOptions<IyzicoConfiguration> config,
        ILogger<IyzicoHttpClient> logger)
    {
        _http   = http;
        _config = config.Value;
        _logger = logger;
    }

    // ── CheckoutForm ──────────────────────────────────────────────────────────

    public Task<IyzicoCheckoutFormResponse?> InitializeCheckoutFormAsync(
        IyzicoCheckoutFormRequest request, CancellationToken ct = default)
        => PostAsync<IyzicoCheckoutFormRequest, IyzicoCheckoutFormResponse>(
            "/payment/iyzipos/checkoutform/initialize/auth/ecom", request, ct);

    public Task<IyzicoRetrieveCheckoutResponse?> RetrieveCheckoutFormAsync(
        IyzicoRetrieveCheckoutRequest request, CancellationToken ct = default)
        => PostAsync<IyzicoRetrieveCheckoutRequest, IyzicoRetrieveCheckoutResponse>(
            "/payment/iyzipos/checkoutform/auth/ecom/detail", request, ct);

    // ── Sub-merchant ──────────────────────────────────────────────────────────

    public Task<IyzicoSubMerchantResponse?> CreateSubMerchantAsync(
        IyzicoSubMerchantRequest request, CancellationToken ct = default)
        => PostAsync<IyzicoSubMerchantRequest, IyzicoSubMerchantResponse>(
            "/v2/submerchants", request, ct);

    // ── Marketplace approval ──────────────────────────────────────────────────

    public Task<IyzicoApprovalResponse?> ApproveMarketplacePaymentAsync(
        IyzicoApprovalRequest request, CancellationToken ct = default)
        => PostAsync<IyzicoApprovalRequest, IyzicoApprovalResponse>(
            "/payment/marketplace/approval", request, ct);

    // ── Refund ────────────────────────────────────────────────────────────────

    public Task<IyzicoRefundResponse?> RefundAsync(
        IyzicoRefundRequest request, CancellationToken ct = default)
        => PostAsync<IyzicoRefundRequest, IyzicoRefundResponse>(
            "/payment/refund", request, ct);

    // ── Core HTTP helper ──────────────────────────────────────────────────────

    private async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path, TRequest body, CancellationToken ct)
        where TResponse : class
    {
        var bodyJson   = JsonSerializer.Serialize(body, JsonOpts);
        var randomKey  = Guid.NewGuid().ToString("N");
        var authHeader = BuildAuthHeader(randomKey, bodyJson);

        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Authorization", authHeader);
        request.Headers.Add("x-iyzi-rnd", randomKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Iyzico HTTP call failed. Path={Path}", path);
            return null;
        }

        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Iyzico returned HTTP {Status} for {Path}. Body={Body}",
                (int)response.StatusCode, path, raw);
            // Still try to deserialize — Iyzico error responses have status/errorCode fields
        }
        else
        {
            _logger.LogDebug("Iyzico {Path} → HTTP {Status}", path, (int)response.StatusCode);
        }

        try
        {
            return JsonSerializer.Deserialize<TResponse>(raw, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize Iyzico response for {Path}. Raw={Raw}", path, raw);
            return null;
        }
    }

    /// <summary>
    /// Builds the IYZWSv2 Authorization header.
    /// Format: IYZWSv2 {base64(apiKey:randomKey:hmac)}
    /// where hmac = base64(HMAC-SHA256(secretKey, apiKey + randomKey + requestBody))
    /// </summary>
    private string BuildAuthHeader(string randomKey, string bodyJson)
    {
        var payload   = _config.ApiKey + randomKey + bodyJson;
        var hmacBytes = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_config.SecretKey),
            Encoding.UTF8.GetBytes(payload));
        var hmacB64   = Convert.ToBase64String(hmacBytes);

        var authValue = $"{_config.ApiKey}:{randomKey}:{hmacB64}";
        var authB64   = Convert.ToBase64String(Encoding.UTF8.GetBytes(authValue));

        return $"IYZWSv2 {authB64}";
    }

    /// <summary>
    /// Validates the HMAC-SHA256 signature Iyzico sends in webhook payloads.
    /// Iyzico webhook signature = SHA256(secretKey + token)
    /// </summary>
    public bool ValidateWebhookSignature(string token, string signature)
    {
        if (string.IsNullOrEmpty(_config.WebhookSecret)) return true; // dev-mode: skip validation

        var expected = Convert.ToBase64String(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(_config.WebhookSecret + token)));

        return string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase);
    }
}
