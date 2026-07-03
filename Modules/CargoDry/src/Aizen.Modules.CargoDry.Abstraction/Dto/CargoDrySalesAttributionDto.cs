using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>Full sales attribution record DTO used in detail and list queries.</summary>
public sealed class CargoDrySalesAttributionDto
{
    public long                            Id                     { get; init; }
    public Guid?                           PublicId               { get; init; }

    // ── Kit identity ────────────────────────────────────────────────────────────
    public long                            KitId                  { get; init; }
    public string                          SerialNumber           { get; init; } = default!;
    public string                          KitCode                { get; init; } = default!;
    public string                          ProductCode            { get; init; } = default!;
    public string?                         BatchCode              { get; init; }

    // ── Commercial context ──────────────────────────────────────────────────────
    public long?                           ProviderProfileId      { get; init; }
    public SalesChannel                    SalesChannel           { get; init; }
    public string                          SalesChannelName       { get; init; } = default!;
    public CargoDryCommercialModel         CommercialModel        { get; init; }
    public string                          CommercialModelName    { get; init; } = default!;
    public long?                           ConsignmentAgreementId { get; init; }
    public long?                           InventoryId            { get; init; }

    // ── Status ──────────────────────────────────────────────────────────────────
    public CargoDrySalesAttributionStatus  Status                 { get; init; }
    public string                          StatusName             { get; init; } = default!;

    // ── Financials ──────────────────────────────────────────────────────────────
    public decimal?                        SalePrice               { get; init; }
    public decimal?                        CommissionRate          { get; init; }
    public decimal?                        CommissionAmount        { get; init; }
    public string?                         CurrencyCode            { get; init; }

    // ── Extended financials (Phase 4A) ──────────────────────────────────────────
    public decimal?                        ProviderShareAmount     { get; init; }
    public decimal?                        PlatformShareAmount     { get; init; }
    public bool                            IsFinanciallyResolved   { get; init; }
    public DateTime?                       FinancialResolvedAtUtc  { get; init; }
    public long?                           FinancialResolvedByUserId { get; init; }
    public string?                         ResolutionNote          { get; init; }

    // ── Settlement link ─────────────────────────────────────────────────────────
    public long?                           SellThroughSettlementId { get; init; }

    // ── Attribution audit ───────────────────────────────────────────────────────
    public DateTime?                       AttributedAt           { get; init; }
    public long?                           AttributedByUserId     { get; init; }

    // ── Review ──────────────────────────────────────────────────────────────────
    public string?                         ReviewNote             { get; init; }
    public long?                           ReviewedByUserId       { get; init; }
    public DateTime?                       ReviewedAt             { get; init; }

    public DateTime                        CreatedAtUtc           { get; init; }
}

/// <summary>Lightweight item used in paged list queries.</summary>
public sealed class CargoDrySalesAttributionListItemDto
{
    public long                            Id                     { get; init; }
    public long                            KitId                  { get; init; }
    public string                          SerialNumber           { get; init; } = default!;
    public string                          KitCode                { get; init; } = default!;
    public string                          ProductCode            { get; init; } = default!;
    public string?                         BatchCode              { get; init; }
    public long?                           ProviderProfileId      { get; init; }
    public SalesChannel                    SalesChannel           { get; init; }
    public string                          SalesChannelName       { get; init; } = default!;
    public CargoDryCommercialModel         CommercialModel        { get; init; }
    public string                          CommercialModelName    { get; init; } = default!;
    public CargoDrySalesAttributionStatus  Status                 { get; init; }
    public string                          StatusName             { get; init; } = default!;
    public decimal?                        SalePrice               { get; init; }
    public decimal?                        CommissionAmount        { get; init; }
    public string?                         CurrencyCode            { get; init; }
    public decimal?                        ProviderShareAmount     { get; init; }
    public bool                            IsFinanciallyResolved   { get; init; }
    public long?                           SellThroughSettlementId { get; init; }
    public DateTime                        CreatedAtUtc            { get; init; }
}

/// <summary>Paged result wrapper for sales attribution list queries.</summary>
public sealed class CargoDrySalesAttributionPagedResultDto
{
    public List<CargoDrySalesAttributionListItemDto> Items { get; init; } = [];
    public int                                       Total { get; init; }
    public int                                       Page  { get; init; }
    public int                                       PageSize { get; init; }
}
