using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
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
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
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
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider,
        ILogger<GetAdminVesselListBffQueryHandler> logger)
    {
        _vessel = vessel;
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
        _logger = logger;
    }

    public override async Task<AdminVesselListBffResponse?> Handle(
        GetAdminVesselListBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselListBffResponse();

        List<VesselListItemBffDto> baseItems;

        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            var authHeader = $"Bearer {serviceToken}";

            var result = await _vessel.GetAdminVesselList(
                authHeader,
                request.UserToken,
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
            _logger.LogDebug("[VesselListBff] location values projected: {Count}", locationCount);
            _logger.LogDebug("[VesselListBff] operational status values projected: {Count}", opStatusCount);

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
        catch (Exception)
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));
            return response;
        }

        // Bulk owner enrichment — one Identity call for all unique owner IDs on the page.
        await TryEnrichOwnerNamesAsync(baseItems, request, response, cancellationToken);

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
        GetAdminVesselListBffQuery request,
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
            _logger.LogDebug("[VesselListBff] ownerUserIds collected: 0 — skipping Identity enrichment");
            return;
        }

        _logger.LogDebug("[VesselListBff] ownerUserIds collected: {Count} / {Ids}",
            ownerUserIds.Length,
            string.Join(",", ownerUserIds));

        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            var authHeader = $"Bearer {serviceToken}";

            // Primary path: enrich by owner user IDs.
            var profileResult = await _identity.GetUserProfilesByUserIds(ownerUserIds, authHeader, request.UserToken);

            _logger.LogDebug("[VesselListBff] Identity profile response success: {Success}, profiles returned: {Count}",
                profileResult?.Header?.IsSuccess,
                profileResult?.Body?.Count ?? 0);

            if (profileResult?.Header?.IsSuccess != true)
            {
                _logger.LogWarning("[VesselListBff] Identity bulk-by-user-ids returned non-success header. IsSuccess={IsSuccess}",
                    profileResult?.Header?.IsSuccess);
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

                var fallbackResult = await _identity.GetUserProfilesByProfileIds(fallbackProfileIds, authHeader, request.UserToken);

                _logger.LogDebug("[VesselListBff] Fallback profile-ID response success: {Success}, profiles returned: {Count}",
                    fallbackResult?.Header?.IsSuccess,
                    fallbackResult?.Body?.Count ?? 0);

                if (fallbackResult?.Header?.IsSuccess == true && fallbackResult.Body?.Count > 0)
                {
                    // Apply by profile ID for the remaining items.
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
