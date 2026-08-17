namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryProductDto
{
    public long    Id            { get; init; }
    public string  ProductCode   { get; init; } = default!;
    public string  Name          { get; init; } = default!;
    public string  Description   { get; init; } = default!;
    public int     ValidityDays  { get; init; }
    public bool    HasSmartDevice{ get; init; }
    public decimal RetailPrice   { get; init; }
    public string  CurrencyCode  { get; init; } = default!;
    public bool    IsActive      { get; init; }
}
