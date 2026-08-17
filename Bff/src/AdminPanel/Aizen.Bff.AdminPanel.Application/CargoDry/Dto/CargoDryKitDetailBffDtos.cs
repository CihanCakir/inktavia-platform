using System.Text.Json.Serialization;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Dto;

/// <summary>
/// Full admin-facing kit record for the GET /admin-panel/cargodry/kits/{id} endpoint.
/// Superset of CargoDryKitBffDto — adds admin-only fields (QR payload, revocation details,
/// consignment link, and entity timestamps).
/// Phase 8B (July 2026).
/// </summary>
[DocumentationInfo("CargoDry kit detail BFF DTO",
    "Returned by GET /admin-panel/cargodry/kits/{id}. Includes all commercial and audit fields.")]
public sealed class CargoDryKitDetailBffDto
{
    public long    Id                { get; init; }
    public string  SerialNumber      { get; init; } = default!;
    public string  KitCode           { get; init; } = default!;
    public string  ProductCode       { get; init; } = default!;
    public string  ProductName       { get; init; } = default!;
    public string  BatchCode         { get; init; } = default!;
    public string  Status            { get; init; } = default!;
    public long?   OwnerUserId       { get; init; }
    public string? OwnerDisplayName  { get; init; }
    public long?   VesselId          { get; init; }
    public string? VesselName        { get; init; }

    [JsonPropertyName("activatedAt")]
    public string? ActivatedDate     { get; init; }

    [JsonPropertyName("expiresAt")]
    public string? ExpiryDate        { get; init; }

    public double  EfficiencyPercent { get; init; }
    public int     DaysUntilExpiry   { get; init; }
    public int     RenewalCount      { get; init; }
    public string  ManufacturedAt    { get; init; } = default!;

    // ── Commercial foundation (Phase 0) ──────────────────────────────────────
    public long?   ProviderProfileId    { get; init; }
    public string? SalesChannel         { get; init; }
    public string? CommercialModel      { get; init; }
    public string? StockLocationType    { get; init; }
    public long?   InvoiceId            { get; init; }
    public long?   PaymentTransactionId { get; init; }
    public long?   WarehouseId          { get; init; }

    // ── Admin-only fields (Phase 8B) ─────────────────────────────────────────
    public long?   ConsignmentAgreementId { get; init; }
    public string? QrPayload              { get; init; }
    public string? RevokeReason           { get; init; }
    public string? RevokedAt              { get; init; }
    public string  CreatedAtUtc           { get; init; } = default!;
    public string? UpdatedAtUtc           { get; init; }
}

/// <summary>
/// Result of GET /admin-panel/cargodry/kits/lookup?q={query}.
/// Returned regardless of whether a kit was found; always check Found before using Kit.
/// Phase 8B (July 2026).
/// </summary>
[DocumentationInfo("CargoDry kit admin lookup result BFF DTO",
    "Exact-match kit lookup by id, kit code, or serial number. Found=false means no match.")]
public sealed class CargoDryKitLookupResultBffDto
{
    public bool                    Found     { get; init; }
    public string                  MatchType { get; init; } = "NotFound";
    public CargoDryKitDetailBffDto? Kit      { get; init; }
    public List<string>            Warnings  { get; init; } = [];
}
