using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.ReactivateProfitProtectionPolicy;

public sealed class ReactivateProfitProtectionPolicyCommand : AizenCommand<ReactivateProfitProtectionPolicyResult>
{
    public required long Id { get; init; }
}

public sealed record ReactivateProfitProtectionPolicyResult(long Id, string? PolicyCode, string Status);
