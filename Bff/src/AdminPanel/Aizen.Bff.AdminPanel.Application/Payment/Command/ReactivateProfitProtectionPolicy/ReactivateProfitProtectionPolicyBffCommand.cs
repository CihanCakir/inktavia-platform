using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivateProfitProtectionPolicy;


// ─── Reactivate ──────────────────────────────────────────────────────────────
public sealed class ReactivateProfitProtectionPolicyBffCommand : AizenCommand<ReactivateProfitProtectionPolicyBffResponse>
{
    public long Id { get; init; }
}
public sealed class ReactivateProfitProtectionPolicyBffResponse { public ProfitProtectionPolicyMutateBffResult Result { get; init; } = default!; }
