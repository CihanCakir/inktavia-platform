using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request offer rejected message", "Published when owner rejects an offer.")]
public sealed class ServiceRequestOfferRejectedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long OfferId { get; set; }
    public long OwnerUserId { get; set; }
    public long ProviderProfileId { get; set; }
    public string? Reason { get; set; }
}
