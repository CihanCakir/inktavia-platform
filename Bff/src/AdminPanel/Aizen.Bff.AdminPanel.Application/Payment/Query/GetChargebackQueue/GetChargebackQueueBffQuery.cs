using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetChargebackQueue;


// ─── Chargeback queue ────────────────────────────────────────────────────────
public sealed class GetChargebackQueueBffQuery : AizenQuery<GetChargebackQueueBffResponse>
{
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
public sealed class GetChargebackQueueBffResponse { public ChargebackQueuePagedDto? Result { get; init; } }
