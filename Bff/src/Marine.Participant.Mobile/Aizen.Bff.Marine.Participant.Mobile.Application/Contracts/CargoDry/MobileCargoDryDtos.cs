using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;

// ── Mobile-facing DTOs (the app never sees the module's commercial/inventory internals) ──────────

/// <summary>Result of a QR/serial scan. When <see cref="IsValid"/> the app holds <see cref="ActivationToken"/>
/// (valid until <see cref="TokenExpiresAt"/>) to complete activation; otherwise <see cref="InvalidReason"/> explains.</summary>
public sealed class MobileKitValidationDto
{
    public bool            IsValid         { get; set; }
    public string?         InvalidReason   { get; set; }
    public string?         ProductName     { get; set; }
    public string?         ProductCode     { get; set; }
    public int             ValidityDays    { get; set; }
    public bool            HasSmartDevice  { get; set; }
    public string?         ActivationToken { get; set; }
    public DateTimeOffset? TokenExpiresAt  { get; set; }
}

/// <summary>An owner's kit — the subset the mobile app needs (no commercial/inventory fields).</summary>
public sealed class MobileKitDto
{
    public long            Id                { get; set; }
    public string          SerialNumber      { get; set; } = default!;
    public string          KitCode           { get; set; } = default!;
    public string          ProductName       { get; set; } = default!;
    public string          Status            { get; set; } = default!;
    public long?           VesselId          { get; set; }
    public string?         VesselName        { get; set; }
    public DateTimeOffset? ActivatedAt       { get; set; }
    public DateTimeOffset? ExpiresAt         { get; set; }
    public double          EfficiencyPercent { get; set; }
    public int             DaysUntilExpiry   { get; set; }
    public int             RenewalCount      { get; set; }
    public bool            HasSmartDevice    { get; set; }
}

/// <summary>The caller's kits plus the roll-up counts the app shows on the kits screen.</summary>
public sealed class MobileMyKitsDto
{
    public List<MobileKitDto> Items         { get; set; } = new();
    public int                Total         { get; set; }
    public int                ActiveCount   { get; set; }
    public int                ExpiringCount { get; set; }
}

/// <summary>Owner kit-detail — the owned list-item enriched (BFF-resolved <see cref="VesselName"/> + the
/// <see cref="IsExpiringSoon"/> convenience flag). Deliberately carries NO admin-sensitive fields (QrPayload,
/// ConsignmentAgreementId, RevokeReason, audit stamps) — it is built from the owner's own kit set, never the admin
/// detail endpoint.</summary>
public sealed class MobileKitDetailDto
{
    public long            Id                { get; set; }
    public string          SerialNumber      { get; set; } = default!;
    public string          KitCode           { get; set; } = default!;
    public string          ProductCode       { get; set; } = default!;
    public string          ProductName       { get; set; } = default!;
    public string          BatchCode         { get; set; } = default!;
    public string          Status            { get; set; } = default!;
    public long?           VesselId          { get; set; }
    public string?         VesselName        { get; set; }
    public DateTimeOffset? ActivatedAt       { get; set; }
    public DateTimeOffset? ExpiresAt         { get; set; }
    public double          EfficiencyPercent { get; set; }
    public int             DaysUntilExpiry   { get; set; }
    public int             RenewalCount      { get; set; }
    public bool            HasSmartDevice    { get; set; }

    /// <summary>True when the kit is Activated and within the module's 30-day expiring window (same threshold the
    /// module's ExpiringCount uses) — so the FE renders the "expiring in N days" banner without re-deriving the rule.</summary>
    public bool IsExpiringSoon { get; set; }
}

// ── Remote request / response bodies (typed — no object/JsonElement over the wire) ───────────────

/// <summary>POST /api/v1/cargodry/public/validate body. Serial + batch identify the kit; signature is optional
/// (smart-device kits).</summary>
public sealed class ValidateKitRemoteRequest
{
    public string  SerialNumber { get; init; } = default!;
    public string  BatchCode    { get; init; } = default!;
    public string? Signature    { get; init; }
}

/// <summary>POST /api/v1/cargodry/kits/activate body. Deliberately carries NO user id — the module stamps
/// <c>UserId = UserInfo.UserId</c> from the BFF-asserted identity, so activate and list share one id-space.</summary>
public sealed class ActivateKitRemoteRequest
{
    public string           ActivationToken { get; init; } = default!;
    public long             VesselId        { get; init; }
    public ActivationMethod Method          { get; init; } = ActivationMethod.QrScan;
}

/// <summary>GET /api/v1/cargodry/kits response. Mirrors the module's <c>GetMyKitsResponse</c> shape (which the
/// module returns raw, not enveloped) so Refit binds the DTO directly.</summary>
public sealed class CargoDryMyKitsRemoteResponse
{
    public List<CargoDryKitDto> Items         { get; init; } = new();
    public int                  Total         { get; init; }
    public int                  ActiveCount   { get; init; }
    public int                  ExpiringCount { get; init; }
}
