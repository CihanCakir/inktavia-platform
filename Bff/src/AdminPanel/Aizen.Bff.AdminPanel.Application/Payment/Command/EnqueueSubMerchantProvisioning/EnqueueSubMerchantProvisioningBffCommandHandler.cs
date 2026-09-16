using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.EnqueueSubMerchantProvisioning;

[DocumentationInfo("Enqueue sub-merchant provisioning BFF command handler",
    "Forwards the admin approve-billing / re-trigger action to the Payment module, which publishes " +
    "ProviderSubMerchantProvisioningRequested and resets the retry counter so a capped/failed profile is picked up " +
    "again. Idempotent — an already-keyed profile is returned as-is. Returns the resulting onboarding status + " +
    "split-eligibility so the UI can refresh the queue row without a second fetch.")]
public sealed class EnqueueSubMerchantProvisioningBffCommandHandler
    : AizenCommandHandler<EnqueueSubMerchantProvisioningBffCommand, SubMerchantOnboardingMutateBffResponse>
{
    private readonly IPaymentRemoteCall _payment;

    public EnqueueSubMerchantProvisioningBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<SubMerchantOnboardingMutateBffResponse?> Handle(
        EnqueueSubMerchantProvisioningBffCommand request, CancellationToken ct)
    {
        var result = await _payment.EnqueueSubMerchantProvisioningAsync(request.ProviderProfileId, ct);
        return new SubMerchantOnboardingMutateBffResponse { Result = result };
    }
}
