using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.CargoDry.Application.Commands.RevokeBatch;

[DocumentationInfo("Revoke CargoDry batch command",
    "Revokes a production batch, marking it as invalid. " +
    "All Available (un-activated) kits in the batch are also revoked automatically.")]
public sealed class RevokeCargoDryBatchCommand : AizenCommand<RevokeCargoDryBatchResponse>
{
    public string BatchCode { get; init; } = default!;
    public string Reason    { get; init; } = default!;
    public long   AdminId   { get; init; }
}
