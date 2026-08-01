using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Offers.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Offers;

/// <summary>
/// Resolves provider identity, computes EligibleServiceBaseAmount from raw line inputs (mirrors S1),
/// then proxies to the Payment internal customer-discount resolver (BE-S6).
/// </summary>
public sealed class GetOfferCustomerDiscountPreviewBffQueryHandler
    : AizenQueryHandler<GetOfferCustomerDiscountPreviewBffQuery, ResolveCustomerDiscountRemoteCallResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IPaymentRemoteCall _payment;
    private readonly ILogger<GetOfferCustomerDiscountPreviewBffQueryHandler> _logger;

    public GetOfferCustomerDiscountPreviewBffQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IPaymentRemoteCall payment,
        ILogger<GetOfferCustomerDiscountPreviewBffQueryHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _payment = payment;
        _logger = logger;
    }

    public override async Task<ResolveCustomerDiscountRemoteCallResponse?> Handle(
        GetOfferCustomerDiscountPreviewBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        // Compute eligible base from raw line inputs (mirrors S1)
        var eligibleBase = OfferPreviewLineCalculator.ComputeEligibleServiceBase(
            request.Lines, request.OfferDiscountAmount);

        var payload = new ResolveCustomerDiscountRemoteCallRequest
        {
            CustomerPlanId            = request.CustomerPlanId,
            CategoryCode              = request.CategoryCode,
            CurrencyCode              = request.CurrencyCode,
            EligibleServiceBaseAmount = eligibleBase,
        };

        try
        {
            return await _payment.ResolveCustomerDiscount(payload);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "ResolveCustomerDiscount preview failed for provider {ProfileId}", _identityHolder.ProfileId);
            throw new AizenBusinessException(ExtractMessage(ex.Content) ?? "Failed to compute customer-discount preview.");
        }
    }

    private static string? ExtractMessage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("header", out var h) && h.TryGetProperty("errorMessage", out var m))
                return m.GetString();
        }
        catch { }
        return null;
    }
}
