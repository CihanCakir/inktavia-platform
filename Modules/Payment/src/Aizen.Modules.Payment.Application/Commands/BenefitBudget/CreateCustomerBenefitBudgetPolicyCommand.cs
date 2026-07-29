using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.CustomerBenefit;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Commands.BenefitBudget;

public sealed class CreateCustomerBenefitBudgetPolicyCommand : AizenCommand<CreateCustomerBenefitBudgetPolicyResult>
{
    public required long                CustomerPlanId      { get; init; }
    public string                       CurrencyCode        { get; init; } = "TRY";
    public required decimal             BenefitBudgetRate   { get; init; }
    public decimal?                     PerPeriodMax        { get; init; }
    public decimal?                     PerCategoryLimit    { get; init; }
    public decimal?                     PerTransactionLimit { get; init; }
    public BenefitRefundRestorePolicy   RefundRestorePolicy { get; init; } = BenefitRefundRestorePolicy.Restore;
    public required DateTime            EffectiveFrom       { get; init; }
    public DateTime?                    EffectiveTo         { get; init; }
    public string?                      Notes               { get; init; }
}

public sealed record CreateCustomerBenefitBudgetPolicyResult(long Id, string PolicyCode);

[DocumentationInfo("CreateCustomerBenefitBudgetPolicyCommandHandler",
    "Admin creates a per-plan benefit budget policy. Validates the rate/limits and runs the single-active overlap guard.")]
public sealed class CreateCustomerBenefitBudgetPolicyCommandHandler
    : AizenCommandHandler<CreateCustomerBenefitBudgetPolicyCommand, CreateCustomerBenefitBudgetPolicyResult>
{
    private readonly ICustomerBenefitBudgetPolicyRepository _policies;
    public CreateCustomerBenefitBudgetPolicyCommandHandler(ICustomerBenefitBudgetPolicyRepository policies)
        => _policies = policies;

    public override async Task<CreateCustomerBenefitBudgetPolicyResult?> Handle(
        CreateCustomerBenefitBudgetPolicyCommand request, CancellationToken ct)
    {
        var policyCode = await _policies.GenerateCodeAsync(ct);

        var policy = CustomerBenefitBudgetPolicyEntity.Create(
            request.CustomerPlanId, request.CurrencyCode, request.BenefitBudgetRate,
            request.PerPeriodMax, request.PerCategoryLimit, request.PerTransactionLimit,
            request.RefundRestorePolicy, request.EffectiveFrom.ToUniversalTime(), request.EffectiveTo?.ToUniversalTime(),
            policyCode, request.Notes);

        var conflict = await _policies.FindOverlappingActivePolicyAsync(policy, ct);
        if (conflict is not null)
            throw new AizenBusinessException(
                (int)PaymentErrorCode.CustomerBenefitBudgetPolicyConflict,
                $"A conflicting active benefit budget policy already exists (Id={conflict.Id}, Code={conflict.PolicyCode}).");

        await _policies.AddAsync(policy, ct);
        return new CreateCustomerBenefitBudgetPolicyResult(policy.Id, policyCode);
    }
}
