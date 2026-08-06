using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user KPI BFF query handler", "Runs parallel Identity calls to aggregate total and pending-verification user counts.")]
public sealed class GetAdminUserKpiBffQueryHandler
    : AizenQueryHandler<GetAdminUserKpiBffQuery, AdminUserKpiBffResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<GetAdminUserKpiBffQueryHandler> _logger;

    public GetAdminUserKpiBffQueryHandler(
        IIdentityRemoteCall identity,
        ILogger<GetAdminUserKpiBffQueryHandler> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    public override async Task<AdminUserKpiBffResponse?> Handle(
        GetAdminUserKpiBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminUserKpiBffResponse();

        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserKpiBff] Failed to acquire Keycloak service token: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        try
        {
            // Four parallel scalar queries — pageSize=1 minimises data transfer while still returning TotalCount.
            var totalTask = _identity.SearchProfiles(pageSize: 1);
            var pendingTask = _identity.SearchProfiles(approvalStatus: "Pending", pageSize: 1);
            var suspendedTask = _identity.SearchProfiles(status: "Inactive", pageSize: 1);
            var activeTodayTask = _identity.GetActiveTodayUserCount();

            await Task.WhenAll(totalTask, pendingTask, suspendedTask, activeTodayTask);

            response.TotalUsers = (int)(totalTask.Result?.Body?.Count ?? 0);
            response.PendingVerification = (int)(pendingTask.Result?.Body?.Count ?? 0);
            response.Suspended = (int)(suspendedTask.Result?.Body?.Count ?? 0);
            response.ActiveToday = activeTodayTask.Result?.Body?.Count ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserKpiBff] Identity KPI aggregation failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }

        return response;
    }
}
