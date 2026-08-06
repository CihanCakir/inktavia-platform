using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

[DocumentationInfo("Get admin conversation detail query handler", "Fetches a full conversation thread with messages for admin oversight.")]
public sealed class GetConversationDetailBffQueryHandler
    : AizenQueryHandler<GetConversationDetailBffQuery, AdminConversationDetailResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetConversationDetailBffQueryHandler(IServiceRequestRemoteCall serviceRequest)
    {
        _serviceRequest = serviceRequest;
    }

    public override async Task<AdminConversationDetailResponse?> Handle(
        GetConversationDetailBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminConversationDetailResponse();

        try
        {

            var result = await _serviceRequest.GetAdminConversationDetail(
                request.ConversationId);
            response.Conversation = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
        }

        return response;
    }
}
