using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetSubMerchantOnboardingQueue;

[DocumentationInfo("Get sub-merchant onboarding queue BFF query handler (BE-I1)",
    "Forwards the paged admin KYC review queue request to the Payment module (optional onboarding status filter). " +
    "Read-only; admin-scoped. No identity enrichment — profiles are admin-managed records.")]
public sealed class GetSubMerchantOnboardingQueueBffQueryHandler
    : AizenQueryHandler<GetSubMerchantOnboardingQueueBffQuery, GetSubMerchantOnboardingQueueBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;

    public GetSubMerchantOnboardingQueueBffQueryHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<GetSubMerchantOnboardingQueueBffResponse?> Handle(
        GetSubMerchantOnboardingQueueBffQuery request, CancellationToken ct)
    {
        var result = await _payment.GetSubMerchantOnboardingQueueAsync(
            status: request.Status, page: request.Page, pageSize: request.PageSize, ct: ct);

        return new GetSubMerchantOnboardingQueueBffResponse { Result = result };
    }
}
