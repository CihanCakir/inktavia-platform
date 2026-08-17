namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

/// <summary>
/// BE-S13a — SR → Payment: read the P10 payment/refund state + cost-free economics for a dispute case file. Pure read.
/// </summary>
public sealed class GetDisputeCasePaymentStateRemoteCallRequest
{
    public required long ServiceRequestId { get; init; }
}
