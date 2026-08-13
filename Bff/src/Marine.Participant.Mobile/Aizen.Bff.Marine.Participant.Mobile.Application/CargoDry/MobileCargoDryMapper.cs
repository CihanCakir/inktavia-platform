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

    public static MobileMyKitsDto MapMyKits(CargoDryMyKitsRemoteResponse r) => new()
    {
        Items         = r.Items?.Select(MapKit).ToList() ?? new List<MobileKitDto>(),
        Total         = r.Total,
        ActiveCount   = r.ActiveCount,
        ExpiringCount = r.ExpiringCount,
    };
}
