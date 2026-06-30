using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;

[DocumentationInfo("Service request message list request", "Pagination and filter for service request message listing.")]
public sealed class ServiceRequestMessageListRequest
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 50;
    public ServiceRequestMessageSenderType? SenderType { get; set; }
    public bool? UnreadOnly { get; set; }
}
