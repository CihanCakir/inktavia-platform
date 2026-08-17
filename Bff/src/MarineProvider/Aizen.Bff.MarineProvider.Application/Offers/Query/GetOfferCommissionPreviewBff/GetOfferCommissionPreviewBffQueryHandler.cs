using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Offers.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Offers;

/// <summary>
/// Resolves the provider identity by-subject, computes per-line CommissionBaseAmount / CommissionEligibility /
/// LineProviderRevenue from the FE's raw inputs (mirroring S1 OfferCalculationService), then proxies to the
/// Payment internal per-line commission resolver (BE-S7). The BFF owns the "server computes totals" contract.
/// </summary>
public sealed class GetOfferCommissionPreviewBffQueryHandler
    : AizenQueryHandler<GetOfferCommissionPreviewBffQuery, ResolveLineCommissionsRemoteCallResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IPaymentRemoteCall _payment;
    private readonly ILogger<GetOfferCommissionPreviewBffQueryHandler> _logger;

    public GetOfferCommissionPreviewBffQueryHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IPaymentRemoteCall payment,
        ILogger<GetOfferCommissionPreviewBffQueryHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _payment = payment;
        _logger = logger;
    }

    public override async Task<ResolveLineCommissionsRemoteCallResponse?> Handle(
        GetOfferCommissionPreviewBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        // Compute per-line economics from raw FE inputs (mirrors S1 OfferCalculationService)
        var computed = OfferPreviewLineCalculator.ComputeLines(request.Lines, request.OfferDiscountAmount);

        var payload = new ResolveLineCommissionsRemoteCallRequest
        {
            ProviderProfileId = _identityHolder.ProfileId!.Value,
            ProviderPlanId    = request.ProviderPlanId,
            CurrencyCode      = request.CurrencyCode,
            Lines             = computed.Select(c =>
            {
                if (!Enum.TryParse<LineCommissionEligibility>(c.CommissionEligibility, ignoreCase: true, out var eligibility))
                    throw new AizenBusinessException($"Invalid commissionEligibility '{c.CommissionEligibility}' for line '{c.LineRef}'.");

                LineType? lineType = Enum.TryParse<LineType>(c.LineType, ignoreCase: true, out var lt) ? lt : null;

                return new ResolveLineCommissionInputDto
                {
                    LineRef               = c.LineRef,
                    LineType              = lineType,
                    ProductCode           = c.ProductCode,
                    CommissionEligibility = eligibility,
                    CategoryCode          = c.CategoryCode,
                    CommissionBaseAmount  = c.CommissionBaseAmount,
                    LineProviderRevenue   = c.LineProviderRevenue,
                };
            }).ToList(),
        };

        try
        {
            return await _payment.ResolveLineCommissions(payload);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "ResolveLineCommissions preview failed for provider {ProfileId}", _identityHolder.ProfileId);
            throw new AizenBusinessException(ExtractMessage(ex.Content) ?? "Failed to compute commission preview.");
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
