using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateProfitProtectionPolicy;


// ─── Create ──────────────────────────────────────────────────────────────────
public sealed class CreateProfitProtectionPolicyBffCommand : AizenCommand<CreateProfitProtectionPolicyBffResponse>
{
    public CreateProfitProtectionPolicyBffRequest Body { get; init; } = default!;
}
public sealed class CreateProfitProtectionPolicyBffResponse { public ProfitProtectionPolicyCreateBffResult Result { get; init; } = default!; }
