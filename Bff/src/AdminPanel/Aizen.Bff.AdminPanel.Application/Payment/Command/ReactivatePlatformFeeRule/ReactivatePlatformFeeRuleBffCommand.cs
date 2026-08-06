using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ReactivatePlatformFeeRule;


// ─── Reactivate ──────────────────────────────────────────────────────────────
public sealed class ReactivatePlatformFeeRuleBffCommand : AizenCommand<ReactivatePlatformFeeRuleBffResponse>
{
    public long Id { get; init; }
}
public sealed class ReactivatePlatformFeeRuleBffResponse { public PlatformFeeRuleMutateBffResult Result { get; init; } = default!; }
