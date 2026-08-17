using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetChargebackQueue;

[DocumentationInfo("Get chargeback queue BFF query handler (P10)",
    "Paged chargeback queue (GET /admin/chargeback-queue). Read-only.")]
public sealed class GetChargebackQueueBffQueryHandler
    : AizenQueryHandler<GetChargebackQueueBffQuery, GetChargebackQueueBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetChargebackQueueBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetChargebackQueueBffResponse?> Handle(GetChargebackQueueBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.GetChargebackQueueAsync(request.Page, request.PageSize, ct) };
}
