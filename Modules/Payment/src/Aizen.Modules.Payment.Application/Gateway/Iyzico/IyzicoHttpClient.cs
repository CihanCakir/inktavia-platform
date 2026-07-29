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

    /// <summary>Create a sub-merchant. BE-P9-fix §5: the body is type-varied (PERSONAL/PRIVATE_COMPANY/LIMITED) upstream.</summary>
    public Task<IyzicoSubMerchantResponse?> CreateSubMerchantAsync(
        IyzicoSubMerchantRequest request, CancellationToken ct = default)
        => PostAsync<IyzicoSubMerchantRequest, IyzicoSubMerchantResponse>(
            "/onboarding/submerchant", request, ct);

    /// <summary>BE-P9-fix §5: update a sub-merchant (PUT /onboarding/submerchant — NO subMerchantType; subMerchantKey + iban).</summary>
    public Task<IyzicoSubMerchantResponse?> UpdateSubMerchantAsync(
        IyzicoUpdateSubMerchantRequest request, CancellationToken ct = default)
        => PutAsync<IyzicoUpdateSubMerchantRequest, IyzicoSubMerchantResponse>(
            "/onboarding/submerchant", request, ct);

    /// <summary>BE-P9-fix §5: retrieve a sub-merchant's full detail by external id (POST /onboarding/submerchant/detail).</summary>
    public Task<IyzicoSubMerchantDetailResponse?> GetSubMerchantDetailAsync(
        IyzicoSubMerchantDetailRequest request, CancellationToken ct = default)
        => PostAsync<IyzicoSubMerchantDetailRequest, IyzicoSubMerchantDetailResponse>(
            "/onboarding/submerchant/detail", request, ct);

    // ── Marketplace approval is item-level only (BE-P9-fix §4): /payment/marketplace/approval does not exist (404). ──
    // ApproveItemAsync (below) is the correct path; ApproveMarketplacePaymentAsync was removed.

    // ── Refund ────────────────────────────────────────────────────────────────

    public Task<IyzicoRefundResponse?> RefundAsync(
        IyzicoRefundRequest request, CancellationToken ct = default)
        => PostAsync<IyzicoRefundRequest, IyzicoRefundResponse>(
            "/payment/refund", request, ct);

    // ── BE-P9 item-level marketplace operations (§10, §21) ─────────────────────

    /// <summary>Approve a single basket item's sub-merchant split (partial/native). Idempotent on iyzico's side.</summary>
    public Task<IyzicoItemApproveResponse?> ApproveItemAsync(
        IyzicoItemApproveRequest request, CancellationToken ct = default)
        => PostAsync<IyzicoItemApproveRequest, IyzicoItemApproveResponse>(
            "/payment/iyzipos/item/approve", request, ct);

    /// <summary>Disapprove a single basket item's sub-merchant split.</summary>
    public Task<IyzicoItemDisapproveResponse?> DisapproveItemAsync(
        IyzicoItemDisapproveRequest request, CancellationToken ct = default)
        => PostAsync<IyzicoItemDisapproveRequest, IyzicoItemDisapproveResponse>(
            "/payment/iyzipos/item/disapprove", request, ct);

    /// <summary>Update a sub-merchant's share on a basket item (change-order / partial per §20.13/§21). PUT /payment/item.</summary>
    public Task<IyzicoUpdateItemResponse?> UpdateSubMerchantShareAsync(
        IyzicoUpdateItemRequest request, CancellationToken ct = default)
        => PutAsync<IyzicoUpdateItemRequest, IyzicoUpdateItemResponse>(
            "/payment/item", request, ct);

    // ── Core HTTP helper ──────────────────────────────────────────────────────

    private Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path, TRequest body, CancellationToken ct)
        where TResponse : class
        => SendJsonAsync<TRequest, TResponse>(HttpMethod.Post, path, body, ct);

    // BE-P9 — same IYZWSv2 signing, PUT verb (for /payment/item).
    private Task<TResponse?> PutAsync<TRequest, TResponse>(
        string path, TRequest body, CancellationToken ct)
        where TResponse : class
        => SendJsonAsync<TRequest, TResponse>(HttpMethod.Put, path, body, ct);

    private async Task<TResponse?> SendJsonAsync<TRequest, TResponse>(
        HttpMethod method, string path, TRequest body, CancellationToken ct)
        where TResponse : class
    {
        var bodyJson   = JsonSerializer.Serialize(body, JsonOpts);
        var randomKey  = Guid.NewGuid().ToString("N");
        // BE-P9-fix §1: IYZWSv2 signs randomKey + uriPath + body → HEX; uriPath (query-less) MUST be included.
        var authHeader = IyzicoSignatureHelper.BuildAuthorizationHeader(
            _config.ApiKey, _config.SecretKey, path, randomKey, bodyJson);

        using var request = new HttpRequestMessage(method, path)
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

    // BE-P9-fix §1: request signing moved to the pure, unit-provable IyzicoSignatureHelper (randomKey+uriPath+body → HEX).

    /// <summary>
    /// BE-P9-fix §2 — validates the CheckoutForm webhook <c>X-IYZ-SIGNATURE-V3</c> (HPP HMACSHA256-HEX over
    /// <c>secretKey + iyziEventType + iyziPaymentId + token + paymentConversationId + status</c>). The dev-bypass (empty
    /// signing secret → accept) is allowed ONLY outside production — <paramref name="isProduction"/> forces validation.
    /// </summary>
    public bool ValidateHppWebhookSignatureV3(
        string iyziEventType, string? iyziPaymentId, string token, string? paymentConversationId, string status,
        string? headerSignature, bool isProduction)
    {
        if (string.IsNullOrEmpty(_config.SecretKey))
        {
            if (isProduction) return false;   // never silently accept in prod
            _logger.LogWarning("Iyzico webhook signature NOT validated — no secret configured (non-prod dev bypass).");
            return true;
        }
        if (string.IsNullOrEmpty(headerSignature)) return false;

        return IyzicoSignatureHelper.ValidateHppWebhookSignatureV3(
            _config.SecretKey, iyziEventType, iyziPaymentId, token, paymentConversationId, status, headerSignature);
    }

    /// <summary>
    /// BE-P9-fix §3 — validates a CF-retrieve response <c>signature</c>
    /// (paymentStatus, paymentId, currency, basketId, conversationId, paidPrice, price, token — prices trailing-zero trimmed).
    /// </summary>
    public bool ValidateCheckoutRetrieveSignature(IyzicoRetrieveCheckoutResponse r)
    {
        if (string.IsNullOrEmpty(_config.SecretKey)) return true;   // dev: no secret → skip (guarded by env upstream)
        return IyzicoSignatureHelper.ValidateResponseSignature(_config.SecretKey, r.Signature,
            r.PaymentStatus ?? "", r.PaymentId ?? "", r.Currency ?? "", r.BasketId ?? "", r.ConversationId ?? "",
            IyzicoSignatureHelper.TrimPrice(r.PaidPrice), IyzicoSignatureHelper.TrimPrice(r.Price), r.Token ?? "");
    }

    /// <summary>BE-P9-fix §3 — validates a refund response <c>signature</c> (paymentId, price, currency, conversationId).</summary>
    public bool ValidateRefundSignature(IyzicoRefundResponse r)
    {
        if (string.IsNullOrEmpty(_config.SecretKey)) return true;
        return IyzicoSignatureHelper.ValidateResponseSignature(_config.SecretKey, r.Signature,
            r.PaymentId ?? "", IyzicoSignatureHelper.TrimPrice(r.Price), r.Currency ?? "", r.ConversationId ?? "");
    }
}
