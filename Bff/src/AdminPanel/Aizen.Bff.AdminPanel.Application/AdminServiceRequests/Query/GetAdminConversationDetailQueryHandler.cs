using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

[DocumentationInfo("Get admin conversation detail query handler", "Fetches a full conversation thread with messages for admin oversight.")]
public sealed class GetAdminConversationDetailQueryHandler
    : AizenQueryHandler<GetAdminConversationDetailQuery, AdminConversationDetailResponse>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminConversationDetailQueryHandler(IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminConversationDetailResponse?> Handle(
        GetAdminConversationDetailQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminConversationDetailResponse();

        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            var authHeader = $"Bearer {serviceToken}";

            var result = await _serviceRequest.GetAdminConversationDetail(
                request.ConversationId, authHeader, request.UserToken);
            response.Conversation = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
        }

        return response;
    }
}
