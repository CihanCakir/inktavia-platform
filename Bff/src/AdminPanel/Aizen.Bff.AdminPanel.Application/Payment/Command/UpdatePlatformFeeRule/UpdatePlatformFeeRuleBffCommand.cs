using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdatePlatformFeeRule;


// ─── Update ──────────────────────────────────────────────────────────────────
public sealed class UpdatePlatformFeeRuleBffCommand : AizenCommand<UpdatePlatformFeeRuleBffResponse>
{
    public long                          Id   { get; init; }
    public UpdatePlatformFeeRuleBffRequest Body { get; init; } = default!;
}
public sealed class UpdatePlatformFeeRuleBffResponse { public PlatformFeeRuleMutateBffResult Result { get; init; } = default!; }
