using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdatePartCommercialTerm;


// ─── PartCommercialTerm: Update (in place; scope + version immutable) ─────────
public sealed class UpdatePartCommercialTermBffCommand : AizenCommand<UpdatePartCommercialTermBffResponse>
{
    public long                              Id   { get; init; }
    public UpdatePartCommercialTermBffRequest Body { get; init; } = default!;
}
public sealed class UpdatePartCommercialTermBffResponse { public PartCommercialTermUpdateBffResult Result { get; init; } = default!; }
