using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Message;

public sealed class GetProviderConversationsResponse
{
    public List<ProviderConversationDto> Conversations { get; init; } = new();
}
