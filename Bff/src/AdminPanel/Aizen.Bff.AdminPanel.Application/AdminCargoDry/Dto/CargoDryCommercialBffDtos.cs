namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;

// ── Sales Attribution BFF DTOs ────────────────────────────────────────────────

public sealed class CargoDrySalesAttributionBffDto
{
    public long     Id                      { get; init; }
    public Guid?    PublicId                { get; init; }
    public long     KitId                   { get; init; }
    public string   SerialNumber            { get; init; } = default!;
    public string   KitCode                 { get; init; } = default!;
    public string   ProductCode             { get; init; } = default!;
    public string?  BatchCode               { get; init; }
    public long?    ProviderProfileId       { get; init; }
    public int      SalesChannel            { get; init; }
    public string   SalesChannelName        { get; init; } = default!;
    public int      CommercialModel         { get; init; }
    public string   CommercialModelName     { get; init; } = default!;
    public long?    ConsignmentAgreementId  { get; init; }
    public long?    InventoryId             { get; init; }
    public int      Status                  { get; init; }
    public string   StatusName              { get; init; } = default!;
    public decimal? SalePrice                 { get; init; }
    public decimal? CommissionRate            { get; init; }
    public decimal? CommissionAmount          { get; init; }
    public string?  CurrencyCode              { get; init; }
    // Phase 4A
    public decimal? ProviderShareAmount       { get; init; }
    public decimal? PlatformShareAmount       { get; init; }
    public bool     IsFinanciallyResolved     { get; init; }
    public DateTime? FinancialResolvedAtUtc   { get; init; }
    public long?    FinancialResolvedByUserId { get; init; }
    public string?  ResolutionNote            { get; init; }
    public long?    SellThroughSettlementId   { get; init; }
    public DateTime? AttributedAt             { get; init; }
    public long?    AttributedByUserId        { get; init; }
    public string?  ReviewNote                { get; init; }
    public long?    ReviewedByUserId          { get; init; }
    public DateTime? ReviewedAt               { get; init; }
    public DateTime CreatedAtUtc              { get; init; }
}

public sealed class CargoDrySalesAttributionListItemBffDto
{
    public long     Id                      { get; init; }
    public long     KitId                   { get; init; }
    public string   SerialNumber            { get; init; } = default!;
    public string   KitCode                 { get; init; } = default!;
    public string   ProductCode             { get; init; } = default!;
    public string?  BatchCode               { get; init; }
    public long?    ProviderProfileId       { get; init; }
    public int      SalesChannel            { get; init; }
    public string   SalesChannelName        { get; init; } = default!;
    public int      CommercialModel         { get; init; }
    public string   CommercialModelName     { get; init; } = default!;
    public int      Status                  { get; init; }
    public string   StatusName              { get; init; } = default!;
    public decimal? SalePrice               { get; init; }
    public decimal? CommissionAmount        { get; init; }
    public string?  CurrencyCode            { get; init; }
    public long?    SellThroughSettlementId { get; init; }
    public DateTime CreatedAtUtc            { get; init; }
}

public sealed class CargoDrySalesAttributionPagedBffDto
{
    public List<CargoDrySalesAttributionListItemBffDto> Items    { get; init; } = [];
    public int                                          Total    { get; init; }
    public int                                          Page     { get; init; }
    public int                                          PageSize { get; init; }
}

// ── Sell-Through Settlement BFF DTOs ─────────────────────────────────────────

public sealed class CargoDrySellThroughSettlementBffDto
{
    public long     Id                      { get; init; }
    public Guid?    PublicId                { get; init; }
    public string   SettlementCode          { get; init; } = default!;
    public long     ConsignmentAgreementId  { get; init; }
    public long     ProviderProfileId       { get; init; }
    public string   ProductCode             { get; init; } = default!;
    public string?  BatchCode               { get; init; }
    public int      TotalKitCount           { get; init; }
    public int      SettledKitCount         { get; init; }
    public decimal  TotalSaleAmount         { get; init; }
    public decimal  TotalCommissionAmount   { get; init; }
    public decimal  ProviderPayoutAmount    { get; init; }
    public string   CurrencyCode            { get; init; } = default!;
    public DateTime PeriodStartUtc          { get; init; }
    public DateTime PeriodEndUtc            { get; init; }
    public int      Status                  { get; init; }
    public string   StatusName              { get; init; } = default!;
    public DateTime? ScheduledSettlementDate { get; init; }
    public DateTime? SettledAtUtc            { get; init; }
    public long?     SettledByUserId         { get; init; }
    public string?   DisputeReason           { get; init; }
    public string?   Note                    { get; init; }
    public DateTime? ReadyForSettlementAtUtc { get; init; }
    public DateTime  CreatedAtUtc            { get; init; }
}

public sealed class CargoDrySellThroughSettlementListItemBffDto
{
    public long     Id                      { get; init; }
    public string   SettlementCode          { get; init; } = default!;
    public long     ConsignmentAgreementId  { get; init; }
    public long     ProviderProfileId       { get; init; }
    public string   ProductCode             { get; init; } = default!;
    public string?  BatchCode               { get; init; }
    public int      TotalKitCount           { get; init; }
    public int      SettledKitCount         { get; init; }
    public decimal  TotalSaleAmount         { get; init; }
    public decimal  ProviderPayoutAmount    { get; init; }
    public string   CurrencyCode            { get; init; } = default!;
    public DateTime PeriodStartUtc          { get; init; }
    public DateTime PeriodEndUtc            { get; init; }
    public int      Status                  { get; init; }
    public string   StatusName              { get; init; } = default!;
    public DateTime? ScheduledSettlementDate { get; init; }
    public DateTime? ReadyForSettlementAtUtc { get; init; }
    public DateTime  CreatedAtUtc            { get; init; }
}

public sealed class CargoDrySellThroughSettlementPagedBffDto
{
    public List<CargoDrySellThroughSettlementListItemBffDto> Items    { get; init; } = [];
    public int                                               Total    { get; init; }
    public int                                               Page     { get; init; }
    public int                                               PageSize { get; init; }
}
