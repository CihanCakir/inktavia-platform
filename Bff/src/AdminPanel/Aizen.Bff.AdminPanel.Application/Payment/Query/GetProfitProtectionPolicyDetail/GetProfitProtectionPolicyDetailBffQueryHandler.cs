using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProfitProtectionPolicyDetail;

[DocumentationInfo("Get profit-protection policy detail BFF query handler (BE-P5)",
    "Fetches a single profit-protection policy by ID from the Payment module (GET /profit-protection/policies/{id}). " +
    "A ProfitProtectionPolicyNotFound surfaces through the envelope. Read-only.")]
public sealed class GetProfitProtectionPolicyDetailBffQueryHandler
    : AizenQueryHandler<GetProfitProtectionPolicyDetailBffQuery, GetProfitProtectionPolicyDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetProfitProtectionPolicyDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetProfitProtectionPolicyDetailBffResponse?> Handle(GetProfitProtectionPolicyDetailBffQuery request, CancellationToken ct)
        => new() { Policy = await _payment.GetProfitProtectionPolicyDetailAsync(request.Id, ct) };
}
