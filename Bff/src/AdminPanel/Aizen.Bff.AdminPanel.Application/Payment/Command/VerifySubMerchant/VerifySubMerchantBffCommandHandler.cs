using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.VerifySubMerchant;

[DocumentationInfo("Verify sub-merchant BFF command handler (BE-I1)",
    "Forwards an admin verify (SubMerchantCreated → Verified) to the Payment module. Returns the resulting onboarding " +
    "status + split-eligibility so the UI can refresh the queue row without a second fetch.")]
public sealed class VerifySubMerchantBffCommandHandler
    : AizenCommandHandler<VerifySubMerchantBffCommand, SubMerchantOnboardingMutateBffResponse>
{
    private readonly IPaymentRemoteCall _payment;

    public VerifySubMerchantBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<SubMerchantOnboardingMutateBffResponse?> Handle(
        VerifySubMerchantBffCommand request, CancellationToken ct)
    {
        var result = await _payment.VerifySubMerchantAsync(request.ProviderProfileId, ct);
        return new SubMerchantOnboardingMutateBffResponse { Result = result };
    }
}
