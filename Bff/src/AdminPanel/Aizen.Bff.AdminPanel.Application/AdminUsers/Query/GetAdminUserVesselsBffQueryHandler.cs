using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user vessels BFF query handler", "Returns paged vessel list for a specific user by querying the Vessel module with ownerUserId filter.")]
public sealed class GetAdminUserVesselsBffQueryHandler
    : AizenQueryHandler<GetAdminUserVesselsBffQuery, AdminUserVesselsBffResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly IVesselRemoteCall _vessel;
    private readonly ILogger<GetAdminUserVesselsBffQueryHandler> _logger;

    public GetAdminUserVesselsBffQueryHandler(
        IIdentityRemoteCall identity,
        IVesselRemoteCall vessel,
        ILogger<GetAdminUserVesselsBffQueryHandler> logger)
    {
        _identity = identity;
        _vessel = vessel;
        _logger = logger;
    }

    public override async Task<AdminUserVesselsBffResponse?> Handle(
        GetAdminUserVesselsBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminUserVesselsBffResponse();

        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserVesselsBff] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        // Resolve Identity userId from profileId (Vessel module uses userId, not profileId).
        long userId;
        try
        {
            var identityResult = await _identity.GetAdminUserProfileDetail(request.ProfileId);
            if (identityResult?.Header?.IsSuccess != true || identityResult.Body == null)
            {
                _logger.LogWarning("[UserVesselsBff] Profile {ProfileId} not found.", request.ProfileId);
                return response;
            }
            userId = identityResult.Body.UserId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserVesselsBff] Identity call failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
            return response;
        }

        try
        {
            var vesselResult = await _vessel.GetAdminVesselList(
pageIndex: 0, pageSize: 50,
                ownerUserId: userId);

            if (vesselResult?.Header?.IsSuccess != true)
            {
                _logger.LogWarning("[UserVesselsBff] Vessel module returned non-success for userId={UserId}.", userId);
                response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));
                return response;
            }

            response.Items = vesselResult.Body?.Vessels?.Items?
                .Select(v => new AdminUserVesselRowBffDto
                {
                    Id = v.Id.ToString(),
                    Name = v.Name,
                    Type = v.VesselTypeCode,
                    FlagCountry = v.FlagCountryCode,
                    RegistrationNo = v.VesselCode,
                    Status = v.Status.ToString(),
                    RegisteredAt = v.CreateDate?.ToString("o") ?? string.Empty
                })
                .ToList() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserVesselsBff] Vessel list fetch failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));
        }

        return response;
    }
}
