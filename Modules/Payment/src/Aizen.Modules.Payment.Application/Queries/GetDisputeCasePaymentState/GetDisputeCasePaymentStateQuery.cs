using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

namespace Aizen.Modules.Payment.Application.Queries.GetDisputeCasePaymentState;

/// <summary>
/// BE-S13a — reads the cost-free P10 payment/refund state + S8 economics for a dispute case file. Pure read.
/// </summary>
public sealed class GetDisputeCasePaymentStateQuery : AizenQuery<GetDisputeCasePaymentStateRemoteCallResponse>
{
    public required GetDisputeCasePaymentStateRemoteCallRequest Request { get; init; }
}
