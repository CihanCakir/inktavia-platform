using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.DeactivateProfitProtectionPolicy;

public sealed class DeactivateProfitProtectionPolicyCommand : AizenCommand<DeactivateProfitProtectionPolicyResult>
{
    public required long Id { get; init; }
}

public sealed record DeactivateProfitProtectionPolicyResult(long Id, string? PolicyCode);
