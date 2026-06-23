using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user KPI BFF query handler", "Runs parallel Identity calls to aggregate total and pending-verification user counts.")]
public sealed class GetAdminUserKpiBffQueryHandler
    : AizenQueryHandler<GetAdminUserKpiBffQuery, AdminUserKpiBffResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    private readonly ILogger<GetAdminUserKpiBffQueryHandler> _logger;

    public GetAdminUserKpiBffQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider,
        ILogger<GetAdminUserKpiBffQueryHandler> logger)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
        _logger = logger;
    }

    public override async Task<AdminUserKpiBffResponse?> Handle(
        GetAdminUserKpiBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminUserKpiBffResponse();

        string authHeader;
        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            authHeader = $"Bearer {serviceToken}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserKpiBff] Failed to acquire Keycloak service token: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        try
        {
            // Three parallel scalar queries — pageSize=1 minimises data transfer while still returning TotalCount.
            var totalTask = _identity.SearchProfiles(authHeader, request.UserToken, pageSize: 1);
            var pendingTask = _identity.SearchProfiles(authHeader, request.UserToken, approvalStatus: "Pending", pageSize: 1);
            var suspendedTask = _identity.SearchProfiles(authHeader, request.UserToken, status: "Inactive", pageSize: 1);

            await Task.WhenAll(totalTask, pendingTask, suspendedTask);

            response.TotalUsers = totalTask.Result?.Body?.TotalCount ?? 0;
            response.PendingVerification = pendingTask.Result?.Body?.TotalCount ?? 0;
            response.Suspended = suspendedTask.Result?.Body?.TotalCount ?? 0;
            // ActiveToday: TODO – requires LastLoginAt tracking from Keycloak event sync (not stored locally).
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserKpiBff] Identity KPI aggregation failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }

        return response;
    }
}
