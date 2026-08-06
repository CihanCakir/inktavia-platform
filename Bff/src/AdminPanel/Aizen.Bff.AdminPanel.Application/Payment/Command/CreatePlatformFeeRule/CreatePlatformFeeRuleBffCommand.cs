using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreatePlatformFeeRule;


// ─── Create ──────────────────────────────────────────────────────────────────
public sealed class CreatePlatformFeeRuleBffCommand : AizenCommand<CreatePlatformFeeRuleBffResponse>
{
    public CreatePlatformFeeRuleBffRequest Body { get; init; } = default!;
}
public sealed class CreatePlatformFeeRuleBffResponse { public PlatformFeeRuleCreateBffResult Result { get; init; } = default!; }
