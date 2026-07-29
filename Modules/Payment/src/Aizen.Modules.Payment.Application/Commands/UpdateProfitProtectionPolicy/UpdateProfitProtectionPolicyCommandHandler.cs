using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.UpdateProfitProtectionPolicy;

[DocumentationInfo("UpdateProfitProtectionPolicyCommandHandler",
    "Admin updates a profit-protection policy's thresholds/window. Re-validates and runs the single-active overlap " +
    "guard (self excluded). Throws ProfitProtectionPolicyNotFound if the policy does not exist.")]
public sealed class UpdateProfitProtectionPolicyCommandHandler
    : AizenCommandHandler<UpdateProfitProtectionPolicyCommand, UpdateProfitProtectionPolicyResult>
{
    private readonly IProfitProtectionPolicyRepository _policies;
    private readonly ILogger<UpdateProfitProtectionPolicyCommandHandler> _logger;

    public UpdateProfitProtectionPolicyCommandHandler(
        IProfitProtectionPolicyRepository policies,
        ILogger<UpdateProfitProtectionPolicyCommandHandler> logger)
    {
        _policies = policies;
        _logger   = logger;
    }

    public override async Task<UpdateProfitProtectionPolicyResult?> Handle(
        UpdateProfitProtectionPolicyCommand request, CancellationToken ct)
    {
        var policy = await _policies.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProfitProtectionPolicyNotFound);

        policy.Update(
            request.MinCustomerSideContributionAmount, request.MinCustomerSideContributionRate,
            request.MinProviderSideContributionAmount, request.MinProviderSideContributionRate,
            request.MinTransactionContributionAmount,  request.MinTransactionContributionRate,
            request.PaymentProcessingExpenseRate, request.PaymentProcessingFixed,
            request.RefundRiskReserveRate,
            request.OtherVariableExpenseRate, request.OtherVariableExpenseFixed,
            request.CustomerSideVariableCostShareRate,
            request.AdjustmentOrder,
            request.EffectiveFrom.ToUniversalTime(), request.EffectiveTo?.ToUniversalTime(),
            request.PolicyName, request.Notes);

        var conflict = await _policies.FindOverlappingActivePolicyAsync(policy, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProfitProtectionPolicyConflict,
                $"The update would conflict with an existing active policy (Id={conflict.Id}, Code={conflict.PolicyCode}).");

        _policies.Update(policy);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation("Profit-protection policy updated. Id={Id} Code={Code}", policy.Id, policy.PolicyCode);
        return new UpdateProfitProtectionPolicyResult(policy.Id, policy.PolicyCode);
    }
}
