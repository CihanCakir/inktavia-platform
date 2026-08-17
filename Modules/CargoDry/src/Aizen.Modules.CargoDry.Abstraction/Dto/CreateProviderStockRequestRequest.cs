namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CreateProviderStockRequestRequest
{
    public string ProductCode { get; init; } = default!;
    public int RequestedQuantity { get; init; }
    public string? Note { get; init; }
}
