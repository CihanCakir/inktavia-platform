using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel list BFF query handler", "Fetches paged vessel list and enriches it with owner display name from Identity (bulk) and human-readable status/location labels.")]
public sealed class GetAdminVesselListBffQueryHandler
    : AizenQueryHandler<GetAdminVesselListBffQuery, AdminVesselListBffResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly ILogger<GetAdminVesselListBffQueryHandler> _logger;

    private static readonly Dictionary<int, string> OperationalStatusLabels = new()
    {
        { 1, "In Service" },
        { 2, "Refit" },
        { 3, "Idle" },
        { 4, "Decommissioned" }
    };

    private static readonly Dictionary<int, string> AssetTypeLabels = new()
    {
        { 1, "Motor Yacht" },
        { 2, "Sailing Yacht" },
        { 3, "Superyacht" },
        { 4, "Catamaran" },
        { 5, "RIB" },
        { 6, "Commercial" }
    };

    private static readonly Dictionary<int, string> OwnershipStatusLabels = new()
    {
        { 1, "Private" },
        { 2, "Charter" },
        { 3, "Corporate" }
    };

    private static readonly Dictionary<int, string> VesselStatusLabels = new()
    {
        { 1, "Draft" },
        { 2, "Active" },
        { 3, "Passive" },
        { 4, "Under Maintenance" },
        { 5, "Sold" },
        { 6, "Archived" }
    };

    public GetAdminVesselListBffQueryHandler(
        IVesselAdminBffRemoteCall vessel,
        IIdentityAdminBffRemoteCall identity,
        ILogger<GetAdminVesselListBffQueryHandler> logger)
    {
        _vessel = vessel;
        _identity = identity;
        _logger = logger;
    }

    public override async Task<AdminVesselListBffResponse?> Handle(
        GetAdminVesselListBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselListBffResponse();

        List<VesselListItemBffDto> baseItems;

        // Acquire the service token once and reuse it for all internal calls in this request.
        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VesselListBff] Failed to acquire Keycloak service token: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        try
        {
            var result = await _vessel.GetAdminVesselList(
                request.PageIndex,
                request.PageSize,
                request.SearchTerm,
                request.IsArchived,
                request.AssetTypes,
                request.OwnershipStatuses,
                request.OperationalStatuses);

            var page = result?.Body?.Vessels;
            if (page == null)
                return response;

            baseItems = page.Items?.Select(v => MapBaseItem(v)).ToList() ?? new();

            var locationCount = baseItems.Count(x => x.LastLocationText != null);
            var opStatusCount = baseItems.Count(x => x.OperationalStatus.HasValue);
            var ownerCount = baseItems.Count(x => x.OwnerUserId.HasValue);
            _logger.LogDebug("[VesselListBff] location values projected: {Count}", locationCount);
            _logger.LogDebug("[VesselListBff] operational status values projected: {Count}", opStatusCount);
            _logger.LogDebug("[VesselListBff] items with ownerUserId projected: {Count} / {Total}", ownerCount, baseItems.Count);

            response.Vessels = new VesselPageBffDto
            {
                From = page.From,
                Index = page.Index,
                Size = page.Size,
                Count = page.Count,
                Pages = page.Pages,
                HasPrevious = page.HasPrevious,
                HasNext = page.HasNext,
                Items = baseItems
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VesselListBff] Vessel module call failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));
            return response;
        }

        // Bulk owner enrichment — one Identity call for all unique owner IDs on the page.
        // Reuses the same service token acquired above.
        await TryEnrichOwnerNamesAsync(baseItems, response, cancellationToken);

        return response;
    }

    private static VesselListItemBffDto MapBaseItem(Aizen.Modules.Vessel.Abstraction.Dto.Vessel.VesselListItemDto v)
    {
        var item = new VesselListItemBffDto
        {
            Id = v.Id,
            VesselCode = v.VesselCode,
            Name = v.Name,
            Slug = v.Slug,
            VesselTypeCode = v.VesselTypeCode,
            FlagCountryCode = v.FlagCountryCode,
            ThumbnailUrl = v.CoverMediaUrl,
            OwnerUserId = v.OwnerUserId,
            OwnerProfileId = v.OwnerProfileId,
            OwnerName = v.OwnerName,
            LengthMeters = v.LengthMeters,
            GrossTonnage = v.GrossTonnage,
            Latitude = v.Latitude,
            Longitude = v.Longitude,
            LastPositionDate = v.LastPositionDate,
            OperationalStatus = v.OperationalStatus,
            AssetType = v.AssetType,
            OwnershipStatus = v.OwnershipStatus,
            Status = (int)v.Status,
            IsArchived = v.IsArchived,
            CreateDate = v.CreateDate
        };

        // Compute human-readable location text.
        item.LastLocationText = ComputeLastLocationText(v.LastLocationMarinaName, v.Latitude, v.Longitude);

        // Map status labels from numeric enums.
        item.OperationalStatusLabel = v.OperationalStatus.HasValue && OperationalStatusLabels.TryGetValue(v.OperationalStatus.Value, out var opLbl) ? opLbl : null;
        item.AssetTypeLabel = v.AssetType.HasValue && AssetTypeLabels.TryGetValue(v.AssetType.Value, out var assetLbl) ? assetLbl : null;
        item.OwnershipStatusLabel = v.OwnershipStatus.HasValue && OwnershipStatusLabels.TryGetValue(v.OwnershipStatus.Value, out var owLbl) ? owLbl : null;
        item.StatusLabel = VesselStatusLabels.TryGetValue((int)v.Status, out var sLbl) ? sLbl : null;

        return item;
    }

    private static string? ComputeLastLocationText(string? marinaName, double? latitude, double? longitude)
    {
        if (!string.IsNullOrWhiteSpace(marinaName))
            return marinaName;

        if (latitude.HasValue && longitude.HasValue)
            return $"{latitude.Value:0.####}, {longitude.Value:0.####}";

        return null;
    }

    private async Task TryEnrichOwnerNamesAsync(
        List<VesselListItemBffDto> items,
        AdminVesselListBffResponse response,
        CancellationToken cancellationToken)
    {
        var ownerUserIds = items
            .Select(x => x.OwnerUserId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();

        if (ownerUserIds.Length == 0)
        {
            _logger.LogWarning("[VesselListBff] ownerUserIds collected: 0 — all vessel items have null OwnerUserId. Check Vessel module projection and cache (5-min TTL). Skipping Identity enrichment.");
            return;
        }

        _logger.LogDebug("[VesselListBff] ownerUserIds collected: {Count} / {Ids}",
            ownerUserIds.Length,
            string.Join(",", ownerUserIds));

        try
        {
            // Primary path: enrich by owner user IDs.
            var profileResult = await _identity.GetUserProfilesByUserIds(ownerUserIds);

            _logger.LogDebug("[VesselListBff] Identity profile response: IsSuccess={Success}, profileCount={Count}, headerNull={HeaderNull}, resultNull={ResultNull}",
                profileResult?.Header?.IsSuccess,
                profileResult?.Body?.Count ?? 0,
                profileResult?.Header is null,
                profileResult is null);

            // AizenHttpClientHandler rewrites non-2xx to 200 when body contains "errors"/"Message",
            // which causes Refit to return a non-AizenApiResponse body → Header becomes null.
            // Treat Header==null as auth/network failure.
            if (profileResult?.Header is null)
            {
                _logger.LogWarning("[VesselListBff] Identity bulk-by-user-ids returned null Header — likely 401/403 from Identity API. " +
                    "Ensure the admin-panel-bff Keycloak service account has 'Admin' realm role OR 'identity.admin' client role on identity-api. " +
                    "Also verify identity-api audience is present in the service token.");
                response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));

                // Attempt profile-ID fallback which uses the same auth — only helps if auth is OK but userId remap is the issue.
                // Skip fallback here since auth itself is likely failing.
                return;
            }

            if (profileResult.Header.IsSuccess != true)
            {
                _logger.LogWarning("[VesselListBff] Identity bulk-by-user-ids returned IsSuccess=false. ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                    profileResult.Header.ErrorCode,
                    profileResult.Header.ErrorMessage);
                response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
                return;
            }

            var profileList = profileResult.Body ?? new List<UserProfileListItemDto>();

            ApplyOwnerNames(items, profileList);

            // Fallback path: for items still missing ownerName that have an ownerProfileId,
            // attempt a secondary lookup by profile ID. This covers the seeder-remap scenario
            // where the user's DB ID differs from the seed ID referenced by vessel owners.
            var stillMissingItems = items
                .Where(x => x.OwnerName == null && x.OwnerProfileId.HasValue)
                .ToList();

            if (stillMissingItems.Count > 0)
            {
                var fallbackProfileIds = stillMissingItems
                    .Select(x => x.OwnerProfileId!.Value)
                    .Distinct()
                    .ToArray();

                _logger.LogDebug("[VesselListBff] Falling back to profile-ID lookup for {Count} items / profileIds: {Ids}",
                    stillMissingItems.Count,
                    string.Join(",", fallbackProfileIds));

                var fallbackResult = await _identity.GetUserProfilesByProfileIds(fallbackProfileIds);

                _logger.LogDebug("[VesselListBff] Fallback profile-ID response success: {Success}, profiles returned: {Count}",
                    fallbackResult?.Header?.IsSuccess,
                    fallbackResult?.Body?.Count ?? 0);

                if (fallbackResult?.Header?.IsSuccess == true && fallbackResult.Body?.Count > 0)
                {
                    var fallbackByProfileId = fallbackResult.Body
                        .GroupBy(p => p.Id)
                        .ToDictionary(g => g.Key, g => g.First());

                    foreach (var item in stillMissingItems)
                    {
                        if (item.OwnerProfileId is long pid && fallbackByProfileId.TryGetValue(pid, out var profile))
                        {
                            item.OwnerName = ResolveOwnerDisplayName(profile);
                            item.OwnerAvatarUrl = profile.ProfilePhotoUrl;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VesselListBff] Identity owner enrichment failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }
    }

    private static void ApplyOwnerNames(List<VesselListItemBffDto> items, List<UserProfileListItemDto> profiles)
    {
        if (profiles.Count == 0) return;

        var profilesByUserId = profiles
            .Where(p => p.UserId > 0)
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var item in items)
        {
            if (item.OwnerUserId is long uid && profilesByUserId.TryGetValue(uid, out var profile))
            {
                item.OwnerName = ResolveOwnerDisplayName(profile);
                item.OwnerAvatarUrl = profile.ProfilePhotoUrl;
                item.OwnerProfileId ??= profile.Id;
            }
        }
    }

    private static string? ResolveOwnerDisplayName(UserProfileListItemDto profile)
    {
        var fullName = $"{profile.FirstName} {profile.LastName}".Trim();
        return !string.IsNullOrWhiteSpace(fullName) ? fullName : null;
    }
}
