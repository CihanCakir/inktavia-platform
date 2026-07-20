using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryRenewalCandidates;

[DocumentationInfo("GetCargoDryRenewalCandidatesQueryHandler",
    "Loads expiring kits from ICargoDryKitRepository, filters out those with open preparations, " +
    "enriches with product pricing, and returns CargoDryRenewalCandidateDto list. " +
    "Phase 11 (July 2026).")]
public sealed class GetCargoDryRenewalCandidatesQueryHandler
    : AizenQueryHandler<GetCargoDryRenewalCandidatesQuery, List<CargoDryRenewalCandidateDto>>
{
    private readonly ICargoDryKitRepository                _kits;
    private readonly ICargoDryProductRepository            _products;
    private readonly ICargoDryRenewalPreparationRepository _preparations;

    public GetCargoDryRenewalCandidatesQueryHandler(
        ICargoDryKitRepository                kits,
        ICargoDryProductRepository            products,
        ICargoDryRenewalPreparationRepository preparations)
    {
        _kits         = kits;
        _products     = products;
        _preparations = preparations;
    }

    public override async Task<List<CargoDryRenewalCandidateDto>> Handle(
        GetCargoDryRenewalCandidatesQuery request, CancellationToken ct)
    {
        // ── Load expiring kits ────────────────────────────────────────────────
        var expiringKits = await _kits.GetExpiringAsync(request.WithinDays, request.ProviderProfileId, ct);

        // ── Load kit IDs that already have an open preparation ─────────────────
        var blockedKitIds = new HashSet<long>(
            await _preparations.GetKitIdsWithOpenPreparationAsync(ct));

        // ── Apply pagination after filtering ───────────────────────────────────
        var skip = (request.Page - 1) * request.PageSize;
        var candidates = expiringKits
            .OrderBy(k => k.ExpiresAt)
            .Skip(skip)
            .Take(request.PageSize)
            .ToList();

        // ── Enrich with products (batch load unique product codes) ─────────────
        var productCodes   = candidates.Select(k => k.ProductCode).Distinct().ToList();
        var productsByCode = new Dictionary<string, Aizen.Modules.CargoDry.Domain.Entities.CargoDryProductEntity>();

        foreach (var code in productCodes)
        {
            var product = await _products.GetByCodeAsync(code, ct);
            if (product is not null)
                productsByCode[code] = product;
        }

        var now = DateTimeOffset.UtcNow;

        return candidates.Select(kit =>
        {
            productsByCode.TryGetValue(kit.ProductCode, out var product);

            var daysUntilExpiry   = kit.ExpiresAt.HasValue
                ? (int)(kit.ExpiresAt.Value - now).TotalDays
                : 0;

            var renewalMonths     = daysUntilExpiry <= 30 ? 1 : daysUntilExpiry <= 60 ? 2 : 3;
            var hasOpenPrep       = blockedKitIds.Contains(kit.Id);
            var blockingReasons   = new List<string>();
            var warnings          = new List<string>();

            if (hasOpenPrep)
                blockingReasons.Add("An open renewal preparation already exists for this kit.");

            if (kit.OwnerUserId is null)
                blockingReasons.Add("Kit has no owner assigned.");

            if (product is null)
                blockingReasons.Add($"Product {kit.ProductCode} not found — cannot determine renewal price.");
            else if (product.RetailPrice <= 0)
                blockingReasons.Add($"Product {kit.ProductCode} has zero or negative RetailPrice.");

            if (daysUntilExpiry < 0)
                warnings.Add("Kit is already expired.");

            return new CargoDryRenewalCandidateDto
            {
                KitId                          = kit.Id,
                KitCode                        = kit.KitCode,
                SerialNumber                   = kit.SerialNumber,
                ProductCode                    = kit.ProductCode,
                ProductName                    = product?.Name,
                BatchCode                      = kit.BatchCode,
                Status                         = kit.Status,
                OwnerUserId                    = kit.OwnerUserId,
                VesselId                       = kit.VesselId,
                ProviderProfileId              = kit.ProviderProfileId,
                ExpiresAtUtc                   = kit.ExpiresAt,
                DaysUntilExpiry                = daysUntilExpiry,
                RecommendedRenewalMonths       = renewalMonths,
                RenewalPrice                   = product?.RetailPrice,
                CurrencyCode                   = product?.CurrencyCode,
                CanPrepareRenewal              = blockingReasons.Count == 0,
                BlockingReasons                = blockingReasons,
                Warnings                       = warnings,
                OpenRenewalPreparationId       = null, // not hydrated here for performance
                RenewalCommission              = product?.ProviderEarningPerSale(),
            };
        }).ToList();
    }
}
