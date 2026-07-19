using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.CargoDry.Application.Commands.CancelProviderStockRequest;

public sealed class CancelProviderStockRequestCommand : AizenCommand<bool>
{
    public long ProviderProfileId { get; init; }
    public long RequestId { get; init; }
    public string? Reason { get; init; }
}
