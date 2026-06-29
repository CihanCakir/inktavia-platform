namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryBatchDto
{
    public long    Id               { get; init; }
    public string  BatchCode        { get; init; } = default!;
    public string  ProductCode      { get; init; } = default!;
    public string  ProductName      { get; init; } = default!;
    public int     TotalKits        { get; init; }
    public string  GeneratedAt      { get; init; } = default!;
    public bool    IsRevoked        { get; init; }
    public string? QrZipFileRef     { get; init; }
    public string? ExcelFileRef     { get; init; }
    public long    CreatedByAdminId { get; init; }
    public string? BatchLabel       { get; init; }
    public string? WarehouseCode    { get; init; }
}

public sealed class CargoDryBatchListDto
{
    public List<CargoDryBatchDto> Items    { get; init; } = [];
    public int                    Total    { get; init; }
    public int                    Page     { get; init; }
    public int                    PageSize { get; init; }
}
