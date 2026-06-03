using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Message;

[DocumentationInfo("Get messages response", "Paginated response for service request message list.")]
public sealed class GetServiceRequestMessagesResponse(List<ServiceRequestMessageDto> messages, int totalCount)
{
    public List<ServiceRequestMessageDto> Messages { get; } = messages;
    public int TotalCount { get; } = totalCount;
}
