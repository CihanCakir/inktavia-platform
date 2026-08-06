using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivatePlatformFeeRule;


// ─── Deactivate ──────────────────────────────────────────────────────────────
public sealed class DeactivatePlatformFeeRuleBffCommand : AizenCommand<DeactivatePlatformFeeRuleBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivatePlatformFeeRuleBffResponse { public PlatformFeeRuleMutateBffResult Result { get; init; } = default!; }
