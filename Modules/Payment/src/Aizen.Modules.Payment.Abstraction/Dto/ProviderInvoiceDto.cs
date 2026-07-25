namespace Aizen.Modules.Payment.Abstraction.Dto;

public sealed class ProviderInvoiceListItemDto
{
    public long    Id             { get; init; }
    public string? InvoiceNumber  { get; init; }
    public int     InvoiceType    { get; init; }
    public int     Status         { get; init; }
    public int     SourceType     { get; init; }
    public long?   SourceId       { get; init; }
    public string  Currency       { get; init; } = "TRY";
    public decimal TotalAmount    { get; init; }
    public decimal TaxAmount      { get; init; }
    public decimal RemainingAmount{ get; init; }
    public bool    HasPdf         { get; init; }
    public DateTime? IssueDateUtc { get; init; }
    public DateTime? DueDateUtc   { get; init; }
    public DateTime? PaidAtUtc    { get; init; }
}

public sealed class ProviderInvoicePagedResultDto
{
    public List<ProviderInvoiceListItemDto> Items { get; init; } = [];
    public int Total    { get; init; }
    public int Page     { get; init; }
    public int PageSize { get; init; }
}

public sealed class ProviderInvoiceLineDto
{
    public string  Description { get; init; } = default!;
    public decimal Quantity    { get; init; }
    public decimal UnitPrice   { get; init; }
    public decimal LineTotal   { get; init; }
    public decimal TaxRate     { get; init; }
    public decimal TaxAmount   { get; init; }
}

public sealed class ProviderInvoiceDetailDto
{
    public long    Id             { get; init; }
    public string? InvoiceNumber  { get; init; }
    public int     InvoiceType    { get; init; }
    public int     Status         { get; init; }
    public int     SourceType     { get; init; }
    public long?   SourceId       { get; init; }
    public string  Currency       { get; init; } = "TRY";
    public decimal TotalAmount    { get; init; }
    public decimal TaxAmount      { get; init; }
    public decimal RemainingAmount{ get; init; }
    public bool    HasPdf         { get; init; }
    public DateTime? IssueDateUtc { get; init; }
    public DateTime? DueDateUtc   { get; init; }
    public DateTime? PaidAtUtc    { get; init; }
    public string  SellerName    { get; init; } = default!;
    public string  BuyerName     { get; init; } = default!;
    public decimal SubTotalAmount{ get; init; }
    public decimal DiscountAmount{ get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal PaidAmount    { get; init; }
    public string? Notes         { get; init; }
    public List<ProviderInvoiceLineDto> Lines { get; init; } = [];
}
