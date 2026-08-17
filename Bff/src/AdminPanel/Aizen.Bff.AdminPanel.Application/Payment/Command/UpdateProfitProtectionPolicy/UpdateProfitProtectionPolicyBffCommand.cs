using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateProfitProtectionPolicy;


// ─── Update ──────────────────────────────────────────────────────────────────
public sealed class UpdateProfitProtectionPolicyBffCommand : AizenCommand<UpdateProfitProtectionPolicyBffResponse>
{
    public long                                   Id   { get; init; }
    public UpdateProfitProtectionPolicyBffRequest Body { get; init; } = default!;
}
public sealed class UpdateProfitProtectionPolicyBffResponse { public ProfitProtectionPolicyMutateBffResult Result { get; init; } = default!; }
