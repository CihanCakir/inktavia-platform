using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Conversation;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;

[DocumentationInfo("Admin conversation detail response", "Full conversation thread with messages for admin oversight.")]
public sealed class AdminConversationDetailResponse
{
    public GetConversationDetailResponse? Conversation { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
