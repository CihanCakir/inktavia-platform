using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPartCommercialTermDetail;

[DocumentationInfo("Get part commercial term detail BFF query handler (BE-S5)",
    "Fetches a single part commercial term by ID from the Payment module (GET /part-commercial-term/rules/{id}). " +
    "A PartCommercialTermNotFound surfaces through the envelope. Admin-only. Read-only.")]
public sealed class GetPartCommercialTermDetailBffQueryHandler
    : AizenQueryHandler<GetPartCommercialTermDetailBffQuery, GetPartCommercialTermDetailBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetPartCommercialTermDetailBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetPartCommercialTermDetailBffResponse?> Handle(GetPartCommercialTermDetailBffQuery request, CancellationToken ct)
        => new() { Term = await _payment.GetPartCommercialTermDetailAsync(request.Id, ct) };
}
