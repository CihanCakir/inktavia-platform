using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CreateProfitProtectionPolicy;

[DocumentationInfo("CreateProfitProtectionPolicyCommandHandler",
    "Admin creates a profit-protection policy. Generates a unique PolicyCode, validates via the domain factory, and " +
    "runs the single-active overlap guard (ProfitProtectionPolicyConflict) before persisting.")]
public sealed class CreateProfitProtectionPolicyCommandHandler
    : AizenCommandHandler<CreateProfitProtectionPolicyCommand, CreateProfitProtectionPolicyResult>
{
    private readonly IProfitProtectionPolicyRepository _policies;
    private readonly ILogger<CreateProfitProtectionPolicyCommandHandler> _logger;

    public CreateProfitProtectionPolicyCommandHandler(
        IProfitProtectionPolicyRepository policies,
        ILogger<CreateProfitProtectionPolicyCommandHandler> logger)
    {
        _policies = policies;
        _logger   = logger;
    }

    public override async Task<CreateProfitProtectionPolicyResult?> Handle(
        CreateProfitProtectionPolicyCommand request, CancellationToken ct)
    {
        var policyCode = await _policies.GenerateCodeAsync(ct);

        var policy = ProfitProtectionPolicyEntity.Create(
            request.CurrencyCode,
            request.MinCustomerSideContributionAmount, request.MinCustomerSideContributionRate,
            request.MinProviderSideContributionAmount, request.MinProviderSideContributionRate,
            request.MinTransactionContributionAmount,  request.MinTransactionContributionRate,
            request.PaymentProcessingExpenseRate, request.PaymentProcessingFixed,
            request.RefundRiskReserveRate,
            request.OtherVariableExpenseRate, request.OtherVariableExpenseFixed,
            request.CustomerSideVariableCostShareRate,
            request.AdjustmentOrder,
            request.EffectiveFrom.ToUniversalTime(), request.EffectiveTo?.ToUniversalTime(),
            policyCode, request.PolicyName, request.Notes,
            request.DefaultLineMinProviderReceivableRate, request.DefaultLineMinProviderReceivableAmount,
            request.DefaultAllowedProviderFundedDiscountRate, request.DefaultAllowedPlatformFundedDiscountRate,
            request.LineCommissionFloorRate, request.MinLinePlatformContributionRate,
            request.StrategicLossExceptionEnabled, request.StrategicLossExceptionMaxLineDeficit);

        var conflict = await _policies.FindOverlappingActivePolicyAsync(policy, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.ProfitProtectionPolicyConflict,
                $"A conflicting active profit-protection policy already exists (Id={conflict.Id}, Code={conflict.PolicyCode}) " +
                "for this currency with an overlapping effective window.");

        await _policies.AddAsync(policy, ct);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation("Profit-protection policy created. Code={Code} Currency={Currency}",
            policyCode, request.CurrencyCode);

        return new CreateProfitProtectionPolicyResult(policy.Id, policyCode);
    }
}
