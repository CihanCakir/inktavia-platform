using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Conversation;

namespace Aizen.Modules.ServiceRequest.Application.Query.Conversation;

[DocumentationInfo("Get conversation list query", "Returns all service request conversations.")]
public sealed class GetConversationListQuery : AizenQuery<GetConversationListResponse>
{
    public string? Filter { get; }
    public GetConversationListQuery(string? filter) => Filter = filter;
}
