using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.DeactivateProfitProtectionPolicy;

[DocumentationInfo("DeactivateProfitProtectionPolicyCommandHandler",
    "Admin deactivates a profit-protection policy (Status=Inactive, IsActive=false). " +
    "Throws ProfitProtectionPolicyNotFound if the policy does not exist.")]
public sealed class DeactivateProfitProtectionPolicyCommandHandler
    : AizenCommandHandler<DeactivateProfitProtectionPolicyCommand, DeactivateProfitProtectionPolicyResult>
{
    private readonly IProfitProtectionPolicyRepository _policies;
    private readonly ILogger<DeactivateProfitProtectionPolicyCommandHandler> _logger;

    public DeactivateProfitProtectionPolicyCommandHandler(
        IProfitProtectionPolicyRepository policies,
        ILogger<DeactivateProfitProtectionPolicyCommandHandler> logger)
    {
        _policies = policies;
        _logger   = logger;
    }

    public override async Task<DeactivateProfitProtectionPolicyResult?> Handle(
        DeactivateProfitProtectionPolicyCommand request, CancellationToken ct)
    {
        var policy = await _policies.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.ProfitProtectionPolicyNotFound);

        policy.Deactivate();
        _policies.Update(policy);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation("Profit-protection policy deactivated. Id={Id} Code={Code}", policy.Id, policy.PolicyCode);
        return new DeactivateProfitProtectionPolicyResult(policy.Id, policy.PolicyCode);
    }
}
