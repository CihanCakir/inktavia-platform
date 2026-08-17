using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

/// <summary>BE-S11b — provider proposed a change order (the owner/admin should review). Nothing financial yet.</summary>
[DocumentationInfo("Service change order proposed message", "Published when a provider proposes a post-acceptance change order.")]
public sealed class ServiceChangeOrderProposedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long ChangeOrderId { get; set; }
    public long AcceptedOfferId { get; set; }
    public long OwnerUserId { get; set; }
    public long ProviderProfileId { get; set; }
    public ServiceChangeOrderDirection Direction { get; set; }
}

/// <summary>BE-S11b — a change order was approved by the customer AND applied (new snapshot + incremental escrow, or a P10 refund).</summary>
[DocumentationInfo("Service change order applied message", "Published when a customer-approved change order is applied.")]
public sealed class ServiceChangeOrderAppliedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long ChangeOrderId { get; set; }
    public long AcceptedOfferId { get; set; }
    public long OwnerUserId { get; set; }
    public long ProviderProfileId { get; set; }
    public ServiceChangeOrderDirection Direction { get; set; }
    public decimal AppliedCustomerTotal { get; set; }
}

/// <summary>BE-S11b — a change order was rejected (customer declined, or the incremental economics breached P5/S9).</summary>
[DocumentationInfo("Service change order rejected message", "Published when a change order is rejected.")]
public sealed class ServiceChangeOrderRejectedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long ChangeOrderId { get; set; }
    public long AcceptedOfferId { get; set; }
    public long OwnerUserId { get; set; }
    public long ProviderProfileId { get; set; }
    public string? Reason { get; set; }
}
