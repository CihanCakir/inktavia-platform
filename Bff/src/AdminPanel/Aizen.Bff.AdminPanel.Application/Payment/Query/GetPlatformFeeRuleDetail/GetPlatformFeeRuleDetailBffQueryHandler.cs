using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPlatformFeeRuleDetail;

[DocumentationInfo("Get platform-fee rule detail BFF query handler (BE-P3)",
    "Fetches a single platform-fee rule by ID from the Payment module (GET /platform-fee/rules/{id}). " +
    "A PlatformFeeRuleNotFound surfaces through the envelope. Read-only.")]
public sealed class GetPlatformFeeRuleDetailBffQueryHandler
    : AizenQueryHandler<GetPlatformFeeRuleDetailBffQuery, GetPlatformFeeRuleDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetPlatformFeeRuleDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetPlatformFeeRuleDetailBffResponse?> Handle(GetPlatformFeeRuleDetailBffQuery request, CancellationToken ct)
        => new() { Rule = await _payment.GetPlatformFeeRuleDetailAsync(request.Id, ct) };
}
