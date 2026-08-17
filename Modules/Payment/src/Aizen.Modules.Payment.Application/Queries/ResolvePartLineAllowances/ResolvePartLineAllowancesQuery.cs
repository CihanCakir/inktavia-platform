using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.Payment.Domain.Entities.PartCommercialTerm;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.ResolvePartLineAllowances;

/// <summary>BE-S5c read-only query: resolve the cost-free part-line allowance for an offer's Product/Consumable lines.</summary>
public sealed class ResolvePartLineAllowancesQuery : AizenQuery<ResolvePartLineAllowancesRemoteCallResponse>
{
    public required ResolvePartLineAllowancesRemoteCallRequest Request { get; init; }
}

[DocumentationInfo("ResolvePartLineAllowancesQueryHandler",
    "Loads the currency's effective-date-filtered active part commercial terms once and resolves the most-specific term per " +
    "line via the pure PartCommercialTermResolver. Projects ONLY the cost-free allowance (max discount + funded split + " +
    "min-receivable) — the confidential SupplierListPrice/ProviderDealerMargin never leave the module. Propagates " +
    "PartCommercialTermConflict on a fail-loud tie.")]
public sealed class ResolvePartLineAllowancesQueryHandler
    : AizenQueryHandler<ResolvePartLineAllowancesQuery, ResolvePartLineAllowancesRemoteCallResponse>
{
    private readonly IPartCommercialTermRepository _terms;
    public ResolvePartLineAllowancesQueryHandler(IPartCommercialTermRepository terms) => _terms = terms;

    public override async Task<ResolvePartLineAllowancesRemoteCallResponse?> Handle(
        ResolvePartLineAllowancesQuery request, CancellationToken ct)
    {
        var req    = request.Request;
        var asOf   = (req.AsOfUtc ?? DateTime.UtcNow).ToUniversalTime();
        var active = await _terms.GetActiveAtAsync(req.CurrencyCode, asOf, ct);

        var lines = req.Lines.Select(l =>
        {
            var ctx = new PartCommercialTermResolveContext(
                ProductCode:       l.ProductCode,
                ProviderProfileId: req.ProviderProfileId,
                Brand:             l.Brand,
                CategoryCode:      l.CategoryCode,
                CurrencyCode:      req.CurrencyCode);

            // Pure resolution (may throw PartCommercialTermConflict — propagate).
            var term = PartCommercialTermResolver.Resolve(active, ctx);

            // Cost-free projection ONLY — SupplierListPrice / ProviderDealerMargin never cross the boundary.
            return term is null
                ? new PartLineAllowanceDto
                {
                    LineRef = l.LineRef, Found = false, ProductCode = l.ProductCode,
                    MaxAllowedCustomerDiscount = 0m, MinimumProviderReceivable = 0m,
                    Funding = new PartFundingSplitDto(),
                }
                : new PartLineAllowanceDto
                {
                    LineRef = l.LineRef, Found = true, ProductCode = l.ProductCode,
                    MaxAllowedCustomerDiscount = term.MaxCustomerDiscount,
                    MinimumProviderReceivable  = term.MinimumProviderReceivable,
                    Funding = new PartFundingSplitDto
                    {
                        SupplierFundedAmount = term.SupplierFundedAmount,
                        ProviderFundedAmount = term.ProviderFundedAmount,
                        PlatformFundedAmount = term.PlatformFundedAmount,
                    },
                };
        }).ToList();

        return new ResolvePartLineAllowancesRemoteCallResponse
        {
            Lines = lines,
            CurrencyCode = req.CurrencyCode.ToUpperInvariant(),
        };
    }
}
