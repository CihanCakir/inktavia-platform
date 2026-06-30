using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Inbound message from the ServiceRequest module when a service request is approved as completed.
/// Payment module listens to trigger escrow release to the provider.
///
/// Published by: Aizen.Modules.ServiceRequest when SR status → CompletionApproved
/// Exchange/queue: payment.service-request.completed
/// </summary>
public sealed class ServiceRequestCompletedMessage : AizenBaseMessage
{
    public long ServiceRequestId  { get; init; }
    public long OfferId           { get; init; }
    public long ProviderProfileId { get; init; }
    public long PayerProfileId    { get; init; }

    /// <summary>Optional admin note to attach to the payout record.</summary>
    public string? AdminNote      { get; init; }

    public DateTime CompletedAtUtc { get; init; }
}
