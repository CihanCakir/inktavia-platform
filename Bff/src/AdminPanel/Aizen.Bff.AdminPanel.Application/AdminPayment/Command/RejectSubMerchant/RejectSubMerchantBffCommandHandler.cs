using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.RejectSubMerchant;

[DocumentationInfo("Reject sub-merchant BFF command handler (BE-I1)",
    "Forwards an admin reject (→ Rejected, not split-eligible) with a typed reason body to the Payment module. " +
    "Returns the resulting onboarding status + split-eligibility.")]
public sealed class RejectSubMerchantBffCommandHandler
    : AizenCommandHandler<RejectSubMerchantBffCommand, SubMerchantOnboardingMutateBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;

    public RejectSubMerchantBffCommandHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<SubMerchantOnboardingMutateBffResponse?> Handle(
        RejectSubMerchantBffCommand request, CancellationToken ct)
    {
        var result = await _payment.RejectSubMerchantAsync(
            request.ProviderProfileId, new RejectSubMerchantBffRequest(request.Reason), ct);
        return new SubMerchantOnboardingMutateBffResponse { Result = result };
    }
}
