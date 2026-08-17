using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreatePartCommercialTerm;


// ─── PartCommercialTerm: Create (= append a new version) ─────────────────────
public sealed class CreatePartCommercialTermBffCommand : AizenCommand<CreatePartCommercialTermBffResponse>
{
    public CreatePartCommercialTermBffRequest Body { get; init; } = default!;
}
public sealed class CreatePartCommercialTermBffResponse { public PartCommercialTermCreateBffResult Result { get; init; } = default!; }
