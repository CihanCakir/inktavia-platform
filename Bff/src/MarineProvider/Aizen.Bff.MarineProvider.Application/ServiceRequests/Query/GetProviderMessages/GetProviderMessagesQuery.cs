using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class ProviderMessagesResponse
{
    public List<ServiceRequestMessageDto> Items { get; init; } = new();
    public bool ChannelOpen { get; init; }
    public int TotalCount { get; init; }
}

public sealed class GetProviderMessagesQuery : AizenQuery<ProviderMessagesResponse>
{
    public long ServiceRequestId { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 50;
}
