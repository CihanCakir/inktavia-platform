using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.CargoDry.Application.Commands.RevokeKit;

public sealed class RevokeKitCommand : AizenCommand<bool>
{
    public long   KitId       { get; init; }
    public string Reason      { get; init; } = default!;
    public long   AdminUserId { get; init; }
}
