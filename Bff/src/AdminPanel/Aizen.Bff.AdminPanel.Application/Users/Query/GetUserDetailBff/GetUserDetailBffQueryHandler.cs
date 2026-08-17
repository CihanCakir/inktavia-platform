using Aizen.Bff.AdminPanel.Application.Users.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.Users.Query;

[DocumentationInfo("Get admin user detail BFF query handler", "Fetches profile detail from Identity + vessel count from Vessel module in sequence.")]
public sealed class GetUserDetailBffQueryHandler
    : AizenQueryHandler<GetUserDetailBffQuery, AdminUserDetailBffResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly IVesselRemoteCall _vessel;
    private readonly ILogger<GetUserDetailBffQueryHandler> _logger;

    public GetUserDetailBffQueryHandler(
        IIdentityRemoteCall identity,
        IVesselRemoteCall vessel,
        ILogger<GetUserDetailBffQueryHandler> logger)
    {
        _identity = identity;
        _vessel = vessel;
        _logger = logger;
    }

    public override async Task<AdminUserDetailBffResponse?> Handle(
        GetUserDetailBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminUserDetailBffResponse();

        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserDetailBff] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        try
        {
            var identityResult = await _identity.GetAdminUserProfileDetail(request.ProfileId);

            if (identityResult?.Header?.IsSuccess != true || identityResult.Body == null)
            {
                _logger.LogWarning("[UserDetailBff] Profile {ProfileId} not found or Identity returned non-success.", request.ProfileId);
                return response; // User is null → controller returns 404
            }

            var p = identityResult.Body;

            // Fetch vessel count separately using the user's Identity userId.
            int vesselCount = 0;
            try
            {
                var vesselResult = await _vessel.GetAdminVesselList(
pageIndex: 0, pageSize: 1,
                    ownerUserId: p.UserId);

                vesselCount = (int)(vesselResult?.Body?.Vessels?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UserDetailBff] Vessel count fetch failed for userId={UserId}: {Message}", p.UserId, ex.Message);
                response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));
            }

            response.User = new AdminUserDetailBffDto
            {
                Id = p.Id.ToString(),
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                EmailVerified = p.EmailConfirmed,
                Phone = p.PhoneNumber,
                Role = AdminUserBffHelpers.MapRole(p.RoleContext),
                Status = AdminUserBffHelpers.MapStatus(p.ApprovalStatus, p.Status),
                IdentityType = p.RoleContext,
                Bio = p.Bio,
                AvatarInitials = AdminUserBffHelpers.ComputeAvatarInitials(p.FirstName, p.LastName),
                LastLoginAt = null, // TODO: not tracked locally; requires Keycloak event sync
                CreatedAt = p.CreateDate?.ToString("o") ?? string.Empty,
                VesselCount = vesselCount,
                TotalTransactions = 0,  // TODO: no payment module remote call
                TotalServiceRequests = 0 // TODO: no userId filter in SR module
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserDetailBff] Identity call failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }

        return response;
    }
}
