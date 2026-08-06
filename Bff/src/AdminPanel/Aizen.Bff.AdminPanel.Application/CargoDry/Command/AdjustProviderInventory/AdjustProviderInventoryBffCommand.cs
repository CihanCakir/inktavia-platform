using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.AdjustProviderInventory;

public sealed class AdjustProviderInventoryBffCommand : AizenCommand<AdjustProviderInventoryBffCommandResponse>
{
    public long    ProviderProfileId  { get; init; }
    public string  ProductCode        { get; init; } = default!;
    public string? BatchCode          { get; init; }
    public int     AdjustmentQuantity { get; init; }
    public string  Reason             { get; init; } = default!;
}

public sealed class AdjustProviderInventoryBffCommandResponse
{
    public CargoDryProviderInventoryBffDto? UpdatedInventory { get; init; }
}
