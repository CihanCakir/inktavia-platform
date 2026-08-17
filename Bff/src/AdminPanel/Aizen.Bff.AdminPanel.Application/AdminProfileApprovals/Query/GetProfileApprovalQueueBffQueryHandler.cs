using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Query;

[DocumentationInfo("Get profile approval queue BFF query handler", "Fetches organizer and venue profiles in parallel from Identity, merges, sorts by submission date, and pages the combined result. Applies server-side approval status filter via Identity query params.")]
public sealed class GetProfileApprovalQueueBffQueryHandler
    : AizenQueryHandler<GetProfileApprovalQueueBffQuery, AdminProfileApprovalQueueBffResponse>
{
    private const int MaxFetchPerType = 200;

    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly ILogger<GetProfileApprovalQueueBffQueryHandler> _logger;

    public GetProfileApprovalQueueBffQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        ILogger<GetProfileApprovalQueueBffQueryHandler> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    public override async Task<AdminProfileApprovalQueueBffResponse?> Handle(
        GetProfileApprovalQueueBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminProfileApprovalQueueBffResponse();

        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ApprovalQueueBff] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        _logger.LogInformation("[ApprovalQueueBff] Profile approval queue requested: status={Status} type={Type} page={Page}",
            request.Status, request.ProfileType, request.PageIndex);

        var identityStatusFilter = MapToIdentityApprovalStatus(request.Status);
        var includeOrganizers = request.ProfileType is null or "all" or "organizer";
        var includeVenues = request.ProfileType is null or "all" or "venue";

        // Fetch organizers and venues in parallel.
        var orgTask = includeOrganizers
            ? FetchOrganizersAsync(identityStatusFilter, cancellationToken)
            : Task.FromResult<(List<OrganizerProfileListItemDto> items, long total)>((new(), 0));

        var venTask = includeVenues
            ? FetchVenuesAsync(identityStatusFilter, cancellationToken)
            : Task.FromResult<(List<VenueProfileListItemDto> items, long total)>((new(), 0));

        await Task.WhenAll(orgTask, venTask);

        var (orgItems, orgTotal) = orgTask.Result;
        var (venItems, venTotal) = venTask.Result;

        if (orgTask.IsFaulted)
            response.Warnings.Add(AdminBffWarning.CallFailed("Identity.Organizers", orgTask.Exception?.Message ?? "Unknown error"));
        if (venTask.IsFaulted)
            response.Warnings.Add(AdminBffWarning.CallFailed("Identity.Venues", venTask.Exception?.Message ?? "Unknown error"));

        // Normalize to queue items.
        var allItems = new List<ProfileApprovalQueueItemBffDto>(orgItems.Count + venItems.Count);
        allItems.AddRange(orgItems.Select(MapOrganizerToQueueItem));
        allItems.AddRange(venItems.Select(MapVenueToQueueItem));

        // Apply BFF-side filters (fields not supported by Identity server-side).
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            allItems = ApplySearchFilter(allItems, request.SearchTerm);

        if (!string.IsNullOrWhiteSpace(request.SubmittedFrom) && DateTime.TryParse(request.SubmittedFrom, out var fromDate))
            allItems = allItems.Where(x => x.SubmittedAt == null || string.Compare(x.SubmittedAt, fromDate.ToString("O"), StringComparison.Ordinal) >= 0).ToList();

        if (!string.IsNullOrWhiteSpace(request.SubmittedTo) && DateTime.TryParse(request.SubmittedTo, out var toDate))
            allItems = allItems.Where(x => x.SubmittedAt == null || string.Compare(x.SubmittedAt, toDate.ToString("O"), StringComparison.Ordinal) <= 0).ToList();

        // Sort merged list by submittedAt descending.
        allItems = allItems
            .OrderByDescending(x => x.SubmittedAt ?? string.Empty)
            .ToList();

        // Compute summary from the full pre-paged list.
        var summary = ComputeSummary(orgItems, venItems, orgTotal, venTotal, request.Status);
        response.Summary = summary;

        // Page the combined result.
        var totalCount = allItems.Count;
        var paged = allItems
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;
        response.Approvals = new ApprovalQueuePageBffDto
        {
            From = request.PageIndex * request.PageSize,
            Index = request.PageIndex,
            Size = request.PageSize,
            Count = totalCount,
            Pages = totalPages,
            HasPrevious = request.PageIndex > 0,
            HasNext = request.PageIndex < totalPages - 1,
            Items = paged
        };

        return response;
    }

    private async Task<(List<OrganizerProfileListItemDto> items, long total)> FetchOrganizersAsync(
        string? approvalStatus, CancellationToken ct)
    {
        try
        {
            var result = await _identity.GetAdminOrganizerProfilesByStatus(
                approvalStatus, pageIndex: 0, pageSize: MaxFetchPerType);

            if (result?.Header?.IsSuccess != true)
            {
                _logger.LogWarning("[ApprovalQueueBff] Organizer list fetch returned non-success: ErrorCode={Code}",
                    result?.Header?.ErrorCode);
                return (new(), 0);
            }

            return (result.Body?.Items ?? new(), result.Body?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ApprovalQueueBff] Organizer list fetch failed from Identity: {Error}", ex.Message);
            throw;
        }
    }

    private async Task<(List<VenueProfileListItemDto> items, long total)> FetchVenuesAsync(
        string? approvalStatus, CancellationToken ct)
    {
        try
        {
            var result = await _identity.GetAdminVenueProfilesByStatus(
                approvalStatus, pageIndex: 0, pageSize: MaxFetchPerType);

            if (result?.Header?.IsSuccess != true)
            {
                _logger.LogWarning("[ApprovalQueueBff] Venue list fetch returned non-success: ErrorCode={Code}",
                    result?.Header?.ErrorCode);
                return (new(), 0);
            }

            return (result.Body?.Items ?? new(), result.Body?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ApprovalQueueBff] Venue list fetch failed from Identity: {Error}", ex.Message);
            throw;
        }
    }

    private static ProfileApprovalQueueItemBffDto MapOrganizerToQueueItem(OrganizerProfileListItemDto item)
    {
        var name = $"{item.FirstName} {item.LastName}".Trim();
        return new ProfileApprovalQueueItemBffDto
        {
            UserId = item.UserId,
            ProfileId = item.Id,
            ProfileType = "organizer",
            ApplicantName = string.IsNullOrEmpty(name) ? null : name,
            CompanyOrVenueName = null,
            Email = null,
            Phone = null,
            City = null,
            Country = null,
            SubmittedAt = item.CreateDate?.ToString("O"),
            ReviewedAt = null,
            Status = MapApprovalStatus(item.ApprovalStatus),
            StatusLabel = item.ApprovalStatus,
            RiskLevel = null,
            DocumentCompletionPercent = null
        };
    }

    private static ProfileApprovalQueueItemBffDto MapVenueToQueueItem(VenueProfileListItemDto item)
    {
        var name = $"{item.FirstName} {item.LastName}".Trim();
        return new ProfileApprovalQueueItemBffDto
        {
            UserId = item.UserId,
            ProfileId = item.Id,
            ProfileType = "venue",
            ApplicantName = string.IsNullOrEmpty(name) ? null : name,
            CompanyOrVenueName = null,
            Email = null,
            Phone = null,
            City = null,
            Country = null,
            SubmittedAt = item.CreateDate?.ToString("O"),
            ReviewedAt = null,
            Status = MapApprovalStatus(item.ApprovalStatus),
            StatusLabel = item.ApprovalStatus,
            RiskLevel = null,
            DocumentCompletionPercent = null
        };
    }

    private static List<ProfileApprovalQueueItemBffDto> ApplySearchFilter(
        List<ProfileApprovalQueueItemBffDto> items, string searchTerm)
    {
        var term = searchTerm.ToLowerInvariant();
        return items.Where(x =>
            (x.ApplicantName?.ToLowerInvariant().Contains(term) == true) ||
            (x.CompanyOrVenueName?.ToLowerInvariant().Contains(term) == true) ||
            (x.Email?.ToLowerInvariant().Contains(term) == true)).ToList();
    }

    private static ProfileApprovalSummaryBffDto ComputeSummary(
        List<OrganizerProfileListItemDto> orgItems,
        List<VenueProfileListItemDto> venItems,
        long orgTotal,
        long venTotal,
        string? statusFilter)
    {
        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);

        var isPendingFilter = statusFilter?.ToLowerInvariant() is "pending" or null;
        var pendingOrgs = isPendingFilter
            ? (int)orgTotal
            : orgItems.Count(x => x.ApprovalStatus?.ToLowerInvariant() == "pending");
        var pendingVen = isPendingFilter
            ? (int)venTotal
            : venItems.Count(x => x.ApprovalStatus?.ToLowerInvariant() == "pending");

        var approvedThisWeek = orgItems.Count(x =>
                x.ApprovalStatus?.ToLowerInvariant() == "approved" &&
                x.CreateDate.HasValue && x.CreateDate.Value >= weekAgo) +
            venItems.Count(x =>
                x.ApprovalStatus?.ToLowerInvariant() == "approved" &&
                x.CreateDate.HasValue && x.CreateDate.Value >= weekAgo);

        var rejectedThisWeek = orgItems.Count(x =>
                x.ApprovalStatus?.ToLowerInvariant() == "rejected" &&
                x.CreateDate.HasValue && x.CreateDate.Value >= weekAgo) +
            venItems.Count(x =>
                x.ApprovalStatus?.ToLowerInvariant() == "rejected" &&
                x.CreateDate.HasValue && x.CreateDate.Value >= weekAgo);

        return new ProfileApprovalSummaryBffDto
        {
            PendingOrganizers = pendingOrgs,
            PendingVenues = pendingVen,
            ApprovedThisWeek = approvedThisWeek,
            RejectedThisWeek = rejectedThisWeek,
            AverageReviewTimeHours = null
        };
    }

    private static string? MapToIdentityApprovalStatus(string? bffStatus) => bffStatus?.ToLowerInvariant() switch
    {
        "pending" => "Pending",
        "approved" => "Approved",
        "rejected" => "Rejected",
        "all" => null,
        _ => null
    };

    private static string MapApprovalStatus(string? identityStatus) => identityStatus?.ToLowerInvariant() switch
    {
        "pending" => "pending",
        "approved" => "approved",
        "rejected" => "rejected",
        _ => "needsReview"
    };
}
