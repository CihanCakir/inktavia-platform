using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Conversation;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;

[DocumentationInfo("Admin conversations response", "List of service request conversations for admin oversight.")]
public sealed class AdminConversationsResponse
{
    public GetConversationListResponse? Conversations { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
