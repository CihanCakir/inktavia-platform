using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;

/// <summary>Projections module CargoDry DTOs → the mobile CargoDry contracts. Enum status is stringified so the
/// app renders a stable name (the module already serializes enums as names). Mirrors <c>MobileVesselMapper</c>.</summary>
public static class MobileCargoDryMapper
{
    public static MobileKitValidationDto MapValidation(CargoDryKitValidationDto v) => new()
    {
        IsValid         = v.IsValid,
        InvalidReason   = v.InvalidReason,
        ProductName     = v.ProductName,
        ProductCode     = v.ProductCode,
        ValidityDays    = v.ValidityDays,
        HasSmartDevice  = v.HasSmartDevice,
        ActivationToken = v.ActivationToken,
        TokenExpiresAt  = v.TokenExpiresAt,
    };

    public static MobileKitDto MapKit(CargoDryKitDto k) => new()
    {
        Id                = k.Id,
        SerialNumber      = k.SerialNumber,
        KitCode           = k.KitCode,
        ProductName       = k.ProductName,
        Status            = k.Status.ToString(),
        VesselId          = k.VesselId,
        VesselName        = k.VesselName,
        ActivatedAt       = k.ActivatedAt,
        ExpiresAt         = k.ExpiresAt,
        EfficiencyPercent = k.EfficiencyPercent,
        DaysUntilExpiry   = k.DaysUntilExpiry,
        RenewalCount      = k.RenewalCount,
        // The module's kit DTO doesn't echo HasSmartDevice (only the validation reply carries it, per product), so
        // it stays false here — the field exists on the mobile contract for the validate flow, not the kit list.
        HasSmartDevice    = false,
        // FIX_MYKITS_CARD_DURATION_CHIP: pass the SKU base duration through so the card chip matches the detail hero.
        ProductValidityDays = k.ProductValidityDays,
    };

    // The module's ExpiringCount uses Activated && DaysUntilExpiry <= 30 — keep the FE from re-deriving the rule.
    private const int ExpiringSoonThresholdDays = 30;

    /// <summary>Owner kit-detail from an owned <see cref="CargoDryKitDto"/>. <paramref name="vesselName"/> is
    /// BFF-resolved from the caller's owned-vessel set (the module's GetMyKits doesn't populate it); null if unknown.</summary>
    public static MobileKitDetailDto MapDetail(CargoDryKitDto k, string? vesselName) => new()
    {
        Id                = k.Id,
        SerialNumber      = k.SerialNumber,
        KitCode           = k.KitCode,
        ProductCode       = k.ProductCode,
        ProductName       = k.ProductName,
        BatchCode         = k.BatchCode,
        Status            = k.Status.ToString(),
        VesselId          = k.VesselId,
        VesselName        = vesselName,
        ActivatedAt       = k.ActivatedAt,
        ExpiresAt         = k.ExpiresAt,
        EfficiencyPercent = k.EfficiencyPercent,
        DaysUntilExpiry   = k.DaysUntilExpiry,
        RenewalCount      = k.RenewalCount,
        HasSmartDevice    = false, // see MapKit — the module's kit DTO doesn't echo this
        IsExpiringSoon    = k.Status == CargoDryKitStatus.Activated && k.DaysUntilExpiry <= ExpiringSoonThresholdDays,
    };

    /// <summary>Per-vessel CargoDry summary from the caller's OWN kit set filtered to one vessel. Owner-safe: the input
    /// is already OwnerUserId-scoped by the module's GetMyKits, so filtering by vessel can never surface a foreign kit.
    /// ActiveCount counts protecting kits (Activated/Renewed), mirroring the FE's active-status set.</summary>
    public static MobileVesselCargoDryDto MapVesselCargoDry(long vesselId, CargoDryMyKitsRemoteResponse r)
    {
        var onVessel = (r.Items ?? new List<CargoDryKitDto>())
            .Where(k => k.VesselId == vesselId)
            .ToList();

        return new MobileVesselCargoDryDto
        {
            ActiveCount = onVessel.Count(k => k.Status is CargoDryKitStatus.Activated or CargoDryKitStatus.Renewed),
            TotalCount  = onVessel.Count,
            Kits        = onVessel.Select(k => new MobileVesselCargoDryKitDto
            {
                Id              = k.Id,
                ProductName     = k.ProductName,
                Status          = k.Status.ToString(),
                ExpiresAt       = k.ExpiresAt,
                DaysUntilExpiry = k.DaysUntilExpiry,
            }).ToList(),
        };
    }

    public static MobileMyKitsDto MapMyKits(CargoDryMyKitsRemoteResponse r) => new()
    {
        Items         = r.Items?.Select(MapKit).ToList() ?? new List<MobileKitDto>(),
        Total         = r.Total,
        ActiveCount   = r.ActiveCount,
        ExpiringCount = r.ExpiringCount,
    };

    /// <summary>Owner-safe product → mobile contract, resolving media file ids to presigned URLs via
    /// <paramref name="urlMap"/> (a missing id → omitted / null thumbnail).</summary>
    public static MobileCargoDryProductDto MapProduct(
        CargoDryProductCatalogDto p, IReadOnlyDictionary<Guid, string> urlMap) => new()
    {
        Id             = p.Id,
        ProductCode    = p.ProductCode,
        Name           = p.Name,
        Description    = p.Description,
        ValidityDays   = p.ValidityDays,
        HasSmartDevice = p.HasSmartDevice,
        DeviceType     = p.DeviceType,
        RetailPrice    = p.RetailPrice,
        CurrencyCode   = p.CurrencyCode,
        ThumbnailUrl   = p.ThumbnailFileId is { } tid && urlMap.TryGetValue(tid, out var turl) ? turl : null,
        ImageUrls      = p.ImageFileIds
            .Where(id => urlMap.ContainsKey(id))
            .Select(id => urlMap[id])
            .ToList(),
    };
}
