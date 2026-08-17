using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ReactivateProfitProtectionPolicy;

[DocumentationInfo("ReactivateProfitProtectionPolicyCommandHandler",
    "Admin re-activates an Inactive profit-protection policy. Calls Reactivate() which sets IsActive=true and re-derives " +
    "Status from the effective dates. Only Inactive policies can be reactivated (throws ProfitProtectionPolicyNotInactive). " +
    "Re-runs the single-active guard (FindOverlappingActivePolicyAsync) before persisting: reactivation must not collide " +
    "with the active policy for that currency (throws ProfitProtectionPolicyConflict). " +
    "Throws ProfitProtectionPolicyNotFound if the policy does not exist.")]
public sealed class ReactivateProfitProtectionPolicyCommandHandler
    : AizenCommandHandler<ReactivateProfitProtectionPolicyCommand, ReactivateProfitProtectionPolicyResult>
{
    private readonly IProfitProtectionPolicyRepository                        _policies;
    private readonly ILogger<ReactivateProfitProtectionPolicyCommandHandler>  _logger;

    public ReactivateProfitProtectionPolicyCommandHandler(
        IProfitProtectionPolicyRepository                        policies,
        ILogger<ReactivateProfitProtectionPolicyCommandHandler>  logger)
    {
        _policies = policies;
        _logger   = logger;
    }

    public override async Task<ReactivateProfitProtectionPolicyResult?> Handle(
        ReactivateProfitProtectionPolicyCommand request, CancellationToken ct)
    {
        var policy = await _policies.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProfitProtectionPolicyNotFound);

        if (policy.Status != CommissionRuleStatus.Inactive)
            throw new AizenBusinessException((int)PaymentErrorCode.ProfitProtectionPolicyNotInactive);

        // Single-active guard: reactivation must not collide with an existing active policy for this currency.
        var conflict = await _policies.FindOverlappingActivePolicyAsync(policy, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProfitProtectionPolicyConflict,
                $"Reactivation would conflict with an existing active profit-protection policy (Id={conflict.Id}, " +
                $"Code={conflict.PolicyCode}) for this currency with an overlapping effective window.");

        policy.Reactivate();
        _policies.Update(policy);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Profit-protection policy reactivated. Id={Id} Code={Code} NewStatus={Status}",
            policy.Id, policy.PolicyCode, policy.Status);

        return new ReactivateProfitProtectionPolicyResult(policy.Id, policy.PolicyCode, policy.Status.ToString());
    }
}
