using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

[DocumentationInfo("Get admin conversations query handler", "Fetches the list of service request conversations for admin oversight.")]
public sealed class GetConversationsBffQueryHandler
    : AizenQueryHandler<GetConversationsBffQuery, AdminConversationsResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetConversationsBffQueryHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AdminConversationsResponse?> Handle(
        GetConversationsBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminConversationsResponse();

        try
        {

            var result = await _serviceRequest.GetAdminConversations(
                request.Filter);
            response.Conversations = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
        }

        return response;
    }
}
