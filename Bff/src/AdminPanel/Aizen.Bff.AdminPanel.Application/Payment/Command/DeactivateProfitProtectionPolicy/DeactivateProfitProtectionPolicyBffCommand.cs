using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateProfitProtectionPolicy;


// ─── Deactivate ──────────────────────────────────────────────────────────────
public sealed class DeactivateProfitProtectionPolicyBffCommand : AizenCommand<DeactivateProfitProtectionPolicyBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivateProfitProtectionPolicyBffResponse { public ProfitProtectionPolicyMutateBffResult Result { get; init; } = default!; }
