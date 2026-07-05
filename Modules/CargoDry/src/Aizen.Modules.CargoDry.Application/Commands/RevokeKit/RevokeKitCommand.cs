using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.RevokeKit;

public sealed class RevokeKitCommand : AizenCommand<RevokeKitResponse>
{
    public long   KitId   { get; init; }
    public string Reason  { get; init; } = default!;
}
