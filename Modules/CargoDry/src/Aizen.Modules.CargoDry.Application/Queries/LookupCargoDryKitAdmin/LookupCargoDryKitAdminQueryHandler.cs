using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryKitDetail;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.LookupCargoDryKitAdmin;

[DocumentationInfo("Lookup CargoDry kit admin query handler",
    "Resolves a kit from a free-form admin query string (numeric id, kit code, or serial number). " +
    "Match priority: numeric id → kit code (exact) → serial number (exact). " +
    "Returns Found=false when nothing matches. " +
    "Phase 8B (July 2026): closes the missing GET /admin/kits/lookup contract gap.")]
public sealed class LookupCargoDryKitAdminQueryHandler
    : AizenQueryHandler<LookupCargoDryKitAdminQuery, LookupCargoDryKitAdminResponse>
{
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;

    public LookupCargoDryKitAdminQueryHandler(
        ICargoDryKitRepository     kits,
        ICargoDryProductRepository products)
    {
        _kits     = kits;
        _products = products;
    }

    public override async Task<LookupCargoDryKitAdminResponse> Handle(
        LookupCargoDryKitAdminQuery request, CancellationToken ct)
    {
        var q = request.Query.Trim();

        CargoDryKitEntity? kit       = null;
        string             matchType = "NotFound";

        // Priority 1: numeric id
        if (long.TryParse(q, out var id))
        {
            kit = await _kits.GetByIdAsync(id, ct);
            if (kit is not null) matchType = "Id";
        }

        // Priority 2: exact kit code
        if (kit is null)
        {
            kit = await _kits.GetByKitCodeAsync(q, ct);
            if (kit is not null) matchType = "KitCode";
        }

        // Priority 3: exact serial number
        if (kit is null)
        {
            kit = await _kits.GetBySerialAsync(q, ct);
            if (kit is not null) matchType = "SerialNumber";
        }

        if (kit is null)
        {
            return new LookupCargoDryKitAdminResponse
            {
                Result = new CargoDryKitLookupResultDto
                {
                    Found     = false,
                    MatchType = "NotFound",
                },
            };
        }

        var product     = await _products.GetByCodeAsync(kit.ProductCode, ct);
        var productName = product?.Name ?? kit.ProductCode;

        var dto      = GetCargoDryKitDetailQueryHandler.MapToDetail(kit, productName);
        var warnings = BuildWarnings(kit);

        return new LookupCargoDryKitAdminResponse
        {
            Result = new CargoDryKitLookupResultDto
            {
                Found     = true,
                MatchType = matchType,
                Kit       = dto,
                Warnings  = warnings,
            },
        };
    }

    private static List<string> BuildWarnings(CargoDryKitEntity kit)
    {
        var warnings = new List<string>();

        if (kit.Status == CargoDryKitStatus.Revoked)
            warnings.Add($"Kit is revoked. Reason: {kit.RevokeReason ?? "No reason recorded."}");

        if (kit.Status == CargoDryKitStatus.Expired)
            warnings.Add("Kit has expired. The owner should initiate a renewal.");

        if (kit.Status == CargoDryKitStatus.Activated && kit.ExpiresAt.HasValue)
        {
            var daysLeft = (kit.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays;
            if (daysLeft is > 0 and <= 30)
                warnings.Add($"Kit expires in {(int)daysLeft} days — renewal may be needed.");
        }

        if (kit.Status == CargoDryKitStatus.Lost)
            warnings.Add("Kit is marked as Lost.");

        return warnings;
    }
}
