using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request offer accepted message", "Published when owner accepts an offer.")]
public sealed class ServiceRequestOfferAcceptedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long OfferId { get; set; }
    public long OwnerUserId { get; set; }
    public long ProviderProfileId { get; set; }
}
