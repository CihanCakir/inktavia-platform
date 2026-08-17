using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.AdjustProviderInventory;

public sealed class AdjustProviderInventoryCommand : AizenCommand<CargoDryProviderInventoryDto>
{
    public long    ProviderProfileId    { get; init; }
    public string  ProductCode          { get; init; } = default!;
    public string? BatchCode            { get; init; }
    public int     AdjustmentQuantity   { get; init; }
    public string  Reason               { get; init; } = default!;
}
