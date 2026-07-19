using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.CreateProviderStockRequest;

public sealed class CreateProviderStockRequestCommand : AizenCommand<CargoDryStockRequestDto>
{
    public long ProviderProfileId { get; init; }
    public string ProductCode { get; init; } = default!;
    public int RequestedQuantity { get; init; }
    public string? Note { get; init; }
}
