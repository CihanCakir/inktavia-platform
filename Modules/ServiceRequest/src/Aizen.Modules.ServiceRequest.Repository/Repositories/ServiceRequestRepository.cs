using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

[DocumentationInfo("ServiceRequest repository", "EF Core implementation of IServiceRequestRepository.")]
public sealed class ServiceRequestRepository : IServiceRequestRepository
{
    private readonly ServiceRequestDbContext _db;

    public ServiceRequestRepository(ServiceRequestDbContext db) => _db = db;

    public Task<ServiceRequestEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<ServiceRequestEntity?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default)
        => _db.ServiceRequests
            .Include(x => x.Items)
            .Include(x => x.StatusHistory)
            .Include(x => x.Attachments)
            .Include(x => x.Messages)
            .Include(x => x.Offers).ThenInclude(o => o.Items)
            .Include(x => x.Offers).ThenInclude(o => o.FxSnapshots)   // BE-S3 — offer-level frozen FX rate rows
            .Include(x => x.Assignment).ThenInclude(a => a!.WorkLogs)
            .Include(x => x.Completion)
            .Include(x => x.Dispute)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<ServiceRequestEntity?> GetByCodeAsync(string requestCode, CancellationToken ct = default)
        => _db.ServiceRequests.FirstOrDefaultAsync(x => x.RequestCode == requestCode && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ServiceRequestEntity>> GetByOwnerUserIdAsync(long ownerUserId, int skip, int take, CancellationToken ct = default)
        => await _db.ServiceRequests
            .AsNoTracking()
            .Where(x => x.OwnerUserId == ownerUserId && !x.IsDeleted)
            .OrderByDescending(x => x.CreateDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountByOwnerUserIdAsync(long ownerUserId, CancellationToken ct = default)
        => _db.ServiceRequests.CountAsync(x => x.OwnerUserId == ownerUserId && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ServiceRequestEntity>> GetByVesselIdAsync(long vesselId, CancellationToken ct = default)
        => await _db.ServiceRequests
            .AsNoTracking()
            .Where(x => x.VesselId == vesselId && !x.IsDeleted)
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceRequestEntity>> GetAdminListAsync(AdminServiceRequestFilterRequest filter, CancellationToken ct = default)
    {
        var skip = filter.PageIndex * filter.PageSize;
        return await BuildAdminQuery(filter)
            .Include(x => x.Assignment)
            .OrderByDescending(x => x.ModifyDate ?? x.CreateDate)
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync(ct);
    }

    public Task<int> CountAdminAsync(AdminServiceRequestFilterRequest filter, CancellationToken ct = default)
        => BuildAdminQuery(filter).CountAsync(ct);

    public async Task<IReadOnlyList<ServiceRequestEntity>> GetOpenForProviderAsync(
        long providerProfileId, ProviderAvailableServiceRequestFilterRequest filter, CancellationToken ct = default)
    {
        var skip = filter.PageIndex * filter.PageSize;
        return await BuildProviderOpenQuery(providerProfileId, filter)
            .Include(x => x.Offers)
            .Include(x => x.Attachments)
            .OrderByDescending(x => x.CreateDate)
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync(ct);
    }

    public Task<int> CountOpenForProviderAsync(
        long providerProfileId, ProviderAvailableServiceRequestFilterRequest filter, CancellationToken ct = default)
        => BuildProviderOpenQuery(providerProfileId, filter).CountAsync(ct);

    public async Task<List<ProviderDiscoveryItemDto>> GetDiscoveryAsync(
        long providerProfileId, ProviderServiceRequestDiscoveryFilter filter, CancellationToken ct = default)
    {
        var biddableStatuses = new[] { ServiceRequestStatus.Open, ServiceRequestStatus.WaitingForOffer, ServiceRequestStatus.OfferReceived };

        var query = _db.ServiceRequests
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                && biddableStatuses.Contains(x.Status)
                && x.Assignment == null);

        // Offer state filter
        var offerState = filter.OfferState ?? OfferStateFilter.Any;
        if (offerState == OfferStateFilter.NotOffered)
            query = query.Where(x => !x.Offers.Any(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted));
        else if (offerState == OfferStateFilter.Offered)
            query = query.Where(x => x.Offers.Any(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted));

        // Category / location / priority / search filters
        if (!string.IsNullOrWhiteSpace(filter.ServiceCategoryCode))
            query = query.Where(x => x.ServiceCategoryCode == filter.ServiceCategoryCode.ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(filter.LocationCityCode))
            query = query.Where(x => x.LocationCityCode == filter.LocationCityCode);

        if (!string.IsNullOrWhiteSpace(filter.LocationCountryCode))
            query = query.Where(x => x.LocationCountryCode == filter.LocationCountryCode);

        if (filter.MinPriority.HasValue)
            query = query.Where(x => x.Priority >= filter.MinPriority.Value);

        // "New only": published after the caller last looked. Indexed on (Status, PublishedAt).
        if (filter.PublishedAfterUtc.HasValue)
            query = query.Where(x => x.PublishedAt != null && x.PublishedAt > filter.PublishedAfterUtc.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(term) ||
                x.RequestCode.ToLower().Contains(term));
        }

        // Geo bounding-box prefilter (indexed)
        if (filter.CenterLatitude.HasValue && filter.CenterLongitude.HasValue && filter.RadiusKm.HasValue)
        {
            var (minLat, maxLat, minLng, maxLng) = ComputeBoundingBox(
                filter.CenterLatitude.Value, filter.CenterLongitude.Value, filter.RadiusKm.Value);
            query = query.Where(x =>
                x.LocationLatitude != null && x.LocationLongitude != null &&
                x.LocationLatitude >= minLat && x.LocationLatitude <= maxLat &&
                x.LocationLongitude >= minLng && x.LocationLongitude <= maxLng);
        }
        else if (filter.BoundsMinLat.HasValue && filter.BoundsMaxLat.HasValue &&
                 filter.BoundsMinLng.HasValue && filter.BoundsMaxLng.HasValue)
        {
            var bMinLat = filter.BoundsMinLat.Value;
            var bMaxLat = filter.BoundsMaxLat.Value;
            var bMinLng = filter.BoundsMinLng.Value;
            var bMaxLng = filter.BoundsMaxLng.Value;
            query = query.Where(x =>
                x.LocationLatitude != null && x.LocationLongitude != null &&
                x.LocationLatitude >= bMinLat && x.LocationLatitude <= bMaxLat &&
                x.LocationLongitude >= bMinLng && x.LocationLongitude <= bMaxLng);
        }

        // Cursor pagination
        var cursor = DecodeCursor(filter.Cursor);
        if (cursor.HasValue)
        {
            var expectedHash = ComputeDiscoveryFilterHash(filter);
            if (cursor.Value.filterHash != expectedHash)
            {
                // Filter changed — ignore stale cursor, start from page 1
            }
            else if (filter.SortBy == "DistanceAsc" && filter.CenterLatitude.HasValue && filter.CenterLongitude.HasValue)
            {
                if (decimal.TryParse(cursor.Value.sortValue, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var lastDistance))
                {
                    var lastId = cursor.Value.lastId;
                    var cLat2 = (double)filter.CenterLatitude.Value;
                    var cLng2 = (double)filter.CenterLongitude.Value;
                    var lastDist = (double)lastDistance;
                    query = query.Where(x =>
                        (x.LocationLatitude == null || x.LocationLongitude == null
                            ? 999999.0
                            : 6371.0 * 2.0 * Math.Asin(Math.Sqrt(
                                Math.Pow(Math.Sin(((double)x.LocationLatitude.Value - cLat2) * Math.PI / 360.0), 2) +
                                Math.Cos((double)x.LocationLatitude.Value * Math.PI / 180.0) * Math.Cos(cLat2 * Math.PI / 180.0) *
                                Math.Pow(Math.Sin(((double)x.LocationLongitude.Value - cLng2) * Math.PI / 360.0), 2)))) > lastDist ||
                        ((x.LocationLatitude == null || x.LocationLongitude == null
                            ? 999999.0
                            : 6371.0 * 2.0 * Math.Asin(Math.Sqrt(
                                Math.Pow(Math.Sin(((double)x.LocationLatitude.Value - cLat2) * Math.PI / 360.0), 2) +
                                Math.Cos((double)x.LocationLatitude.Value * Math.PI / 180.0) * Math.Cos(cLat2 * Math.PI / 180.0) *
                                Math.Pow(Math.Sin(((double)x.LocationLongitude.Value - cLng2) * Math.PI / 360.0), 2)))) == lastDist
                            && x.Id > lastId));
                }
            }
            else if (filter.SortBy == "PriorityDesc")
            {
                if (System.Enum.TryParse<ServiceRequestPriority>(cursor.Value.sortValue, out var lastPriority))
                {
                    var lastId = cursor.Value.lastId;
                    query = query.Where(x =>
                        x.Priority < lastPriority ||
                        (x.Priority == lastPriority && x.Id < lastId));
                }
            }
            else // PublishedAtDesc
            {
                if (DateTime.TryParse(cursor.Value.sortValue, null, System.Globalization.DateTimeStyles.RoundtripKind, out var lastPublishedAt))
                {
                    var lastId = cursor.Value.lastId;
                    query = query.Where(x =>
                        x.PublishedAt < lastPublishedAt ||
                        (x.PublishedAt == lastPublishedAt && x.Id < lastId));
                }
            }
        }

        // Haversine distance expression (reused for projection and sorting)
        var hasCentre = filter.CenterLatitude.HasValue && filter.CenterLongitude.HasValue;

        // Sorting
        IOrderedQueryable<ServiceRequestEntity> ordered;
        if (filter.SortBy == "DistanceAsc" && hasCentre)
        {
            var cLat = (double)filter.CenterLatitude!.Value;
            var cLng = (double)filter.CenterLongitude!.Value;
            ordered = query
                .OrderBy(x => x.LocationLatitude == null || x.LocationLongitude == null
                    ? 999999.0
                    : 6371.0 * 2.0 * Math.Asin(Math.Sqrt(
                        Math.Pow(Math.Sin(((double)x.LocationLatitude.Value - cLat) * Math.PI / 360.0), 2) +
                        Math.Cos((double)x.LocationLatitude.Value * Math.PI / 180.0) * Math.Cos(cLat * Math.PI / 180.0) *
                        Math.Pow(Math.Sin(((double)x.LocationLongitude.Value - cLng) * Math.PI / 360.0), 2))))
                .ThenBy(x => x.Id);
        }
        else if (filter.SortBy == "PriorityDesc")
            ordered = query.OrderByDescending(x => x.Priority).ThenByDescending(x => x.Id);
        else
            ordered = query.OrderByDescending(x => x.PublishedAt).ThenByDescending(x => x.Id);

        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        // Select projection — NO Include
        return await ordered
            .Take(pageSize + 1)
            .Select(x => new ProviderDiscoveryItemDto
            {
                Id = x.Id,
                RequestCode = x.RequestCode,
                Title = x.Title,
                Description = x.Description,
                Status = x.Status.ToString(),
                Priority = x.Priority.ToString(),
                ServiceCategoryCode = x.ServiceCategoryCode,
                ServiceTypeCode = x.ServiceTypeCode,
                LocationCityCode = x.LocationCityCode,
                LocationCountryCode = x.LocationCountryCode,
                LocationMarinaName = x.LocationMarinaName,
                // Snapped in the SQL projection — exact coordinates never leave the repository.
                // Grid: 0.005° (~500m). Deterministic per-row jitter from Id so markers stay
                // stable across calls but don't cluster on grid intersections.
                SnappedLatitude = x.LocationLatitude == null
                    ? (decimal?)null
                    : (decimal)(Math.Round(((double)x.LocationLatitude.Value + (x.Id % 7 - 3) * 0.001) / 0.005) * 0.005),
                SnappedLongitude = x.LocationLongitude == null
                    ? (decimal?)null
                    : (decimal)(Math.Round(((double)x.LocationLongitude.Value + (x.Id % 7 - 3) * 0.001) / 0.005) * 0.005),
                // Haversine distance computed in SQL (null when no centre)
                DistanceKm = (hasCentre && x.LocationLatitude != null && x.LocationLongitude != null)
                    ? (decimal?)(6371.0 * 2.0 * Math.Asin(Math.Sqrt(
                        Math.Pow(Math.Sin(((double)x.LocationLatitude.Value - (double)filter.CenterLatitude!.Value) * Math.PI / 360.0), 2) +
                        Math.Cos((double)x.LocationLatitude.Value * Math.PI / 180.0) * Math.Cos((double)filter.CenterLatitude.Value * Math.PI / 180.0) *
                        Math.Pow(Math.Sin(((double)x.LocationLongitude.Value - (double)filter.CenterLongitude!.Value) * Math.PI / 360.0), 2))))
                    : null,
                RequestedStartDate = x.RequestedStartDate,
                RequestedEndDate = x.RequestedEndDate,
                ExpiresAt = x.ExpiresAt,
                PublishedAt = x.PublishedAt,
                // Vessel
                VesselId = x.VesselId,
                VesselName = x.VesselName,
                // Counts (subqueries)
                OfferCount = x.Offers.Count(o => !o.IsDeleted),
                AttachmentCount = x.Attachments.Count(a => !a.IsDeleted),
                // Caller's own offer state
                HasProviderOffer = x.Offers.Any(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted),
                ProviderOfferId = x.Offers
                    .Where(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted)
                    .Select(o => (long?)o.Id)
                    .FirstOrDefault(),
                ProviderOfferStatus = x.Offers
                    .Where(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted)
                    .Select(o => o.Status.ToString())
                    .FirstOrDefault(),
                ProviderOfferTotalAmount = x.Offers
                    .Where(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted)
                    .Select(o => (decimal?)o.TotalAmount)
                    .FirstOrDefault(),
                // Precise: set only by the owner-edit command (UpdateProfile), never by publish.
                IsUpdated = x.ContentUpdatedAt != null && x.PublishedAt != null && x.ContentUpdatedAt > x.PublishedAt,
            })
            .ToListAsync(ct);
    }

    public async Task<List<DiscoveryMarkerDto>> GetDiscoveryMarkersAsync(
        long providerProfileId, ProviderServiceRequestDiscoveryFilter filter, CancellationToken ct = default)
    {
        var query = BuildDiscoveryBaseQuery(providerProfileId, filter);

        // Bounds filter (required for markers — handler validates)
        if (filter.BoundsMinLat.HasValue && filter.BoundsMaxLat.HasValue &&
            filter.BoundsMinLng.HasValue && filter.BoundsMaxLng.HasValue)
        {
            var bMinLat = filter.BoundsMinLat.Value;
            var bMaxLat = filter.BoundsMaxLat.Value;
            var bMinLng = filter.BoundsMinLng.Value;
            var bMaxLng = filter.BoundsMaxLng.Value;
            query = query.Where(x =>
                x.LocationLatitude != null && x.LocationLongitude != null &&
                x.LocationLatitude >= bMinLat && x.LocationLatitude <= bMaxLat &&
                x.LocationLongitude >= bMinLng && x.LocationLongitude <= bMaxLng);
        }

        // UtcNow.Date is Kind=Unspecified — force Utc for the x.PublishedAt >= todayUtc timestamptz comparison below.
        var todayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        return await query
            .Take(501)
            .Select(x => new DiscoveryMarkerDto
            {
                Id = x.Id,
                Title = x.Title,
                LocationMarinaName = x.LocationMarinaName,
                Priority = x.Priority.ToString(),
                ApproxLatitude = x.LocationLatitude == null
                    ? (decimal?)null
                    : (decimal)(Math.Round(((double)x.LocationLatitude.Value + (x.Id % 7 - 3) * 0.001) / 0.005) * 0.005),
                ApproxLongitude = x.LocationLongitude == null
                    ? (decimal?)null
                    : (decimal)(Math.Round(((double)x.LocationLongitude.Value + (x.Id % 7 - 3) * 0.001) / 0.005) * 0.005),
                IsNew = x.PublishedAt >= todayUtc,
                HasProviderOffer = x.Offers.Any(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted),
                ProviderOfferStatus = x.Offers
                    .Where(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted)
                    .Select(o => o.Status.ToString())
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);
    }

    public async Task<ProviderDiscoverySummaryResponse> GetDiscoverySummaryAsync(
        long providerProfileId, ProviderServiceRequestDiscoveryFilter filter, CancellationToken ct = default)
    {
        var query = BuildDiscoveryBaseQuery(providerProfileId, filter);

        // Geo bounding-box prefilter
        if (filter.CenterLatitude.HasValue && filter.CenterLongitude.HasValue && filter.RadiusKm.HasValue)
        {
            var (minLat, maxLat, minLng, maxLng) = ComputeBoundingBox(
                filter.CenterLatitude.Value, filter.CenterLongitude.Value, filter.RadiusKm.Value);
            query = query.Where(x =>
                x.LocationLatitude != null && x.LocationLongitude != null &&
                x.LocationLatitude >= minLat && x.LocationLatitude <= maxLat &&
                x.LocationLongitude >= minLng && x.LocationLongitude <= maxLng);
        }
        else if (filter.BoundsMinLat.HasValue && filter.BoundsMaxLat.HasValue &&
                 filter.BoundsMinLng.HasValue && filter.BoundsMaxLng.HasValue)
        {
            var bMinLat = filter.BoundsMinLat.Value;
            var bMaxLat = filter.BoundsMaxLat.Value;
            var bMinLng = filter.BoundsMinLng.Value;
            var bMaxLng = filter.BoundsMaxLng.Value;
            query = query.Where(x =>
                x.LocationLatitude != null && x.LocationLongitude != null &&
                x.LocationLatitude >= bMinLat && x.LocationLatitude <= bMaxLat &&
                x.LocationLongitude >= bMinLng && x.LocationLongitude <= bMaxLng);
        }

        // UtcNow.Date is Kind=Unspecified — force Utc for the x.PublishedAt >= todayUtc timestamptz comparison below.
        var todayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        var openCount = await query.CountAsync(ct);
        var publishedTodayCount = await query.CountAsync(x => x.PublishedAt >= todayUtc, ct);
        var emergencyCount = await query.CountAsync(x => x.Priority >= ServiceRequestPriority.Urgent, ct);
        var myActiveOfferCount = await query.CountAsync(x =>
            x.Offers.Any(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted &&
                (o.Status == ServiceRequestOfferStatus.Submitted || o.Status == ServiceRequestOfferStatus.UnderReview)), ct);

        return new ProviderDiscoverySummaryResponse
        {
            OpenCount = openCount,
            PublishedTodayCount = publishedTodayCount,
            EmergencyCount = emergencyCount,
            MyActiveOfferCount = myActiveOfferCount,
        };
    }

    public Task AddAsync(ServiceRequestEntity entity, CancellationToken ct = default)
        => _db.ServiceRequests.AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestEntity entity) => _db.ServiceRequests.Update(entity);

    private static (string sortValue, long lastId, string filterHash)? DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split('|', 3);
            if (parts.Length != 3 || !long.TryParse(parts[1], out var lastId))
                return null;

            return (parts[0], lastId, parts[2]);
        }
        catch
        {
            return null;
        }
    }

    private static string ComputeDiscoveryFilterHash(ProviderServiceRequestDiscoveryFilter filter)
    {
        var hashInput = new
        {
            filter.SortBy,
            filter.LocationCityCode,
            filter.LocationCountryCode,
            filter.ServiceCategoryCode,
            filter.MinPriority,
            filter.SearchTerm,
            filter.OfferState,
            filter.CenterLatitude,
            filter.CenterLongitude,
            filter.RadiusKm,
            filter.BoundsMinLat,
            filter.BoundsMaxLat,
            filter.BoundsMinLng,
            filter.BoundsMaxLng,
        };

        var json = JsonSerializer.Serialize(hashInput);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes)[..16];
    }

    /// <summary>
    /// Computes a bounding box from centre + radius. Duplicated from GeoHelper to avoid
    /// Repository → Application dependency. Kept small and self-contained.
    /// </summary>
    private static (decimal minLat, decimal maxLat, decimal minLng, decimal maxLng) ComputeBoundingBox(
        decimal centerLat, decimal centerLng, decimal radiusKm)
    {
        const double EarthRadiusKm = 6371.0;
        var latRad = (double)centerLat * Math.PI / 180.0;
        var deltaLat = (double)radiusKm / EarthRadiusKm * (180.0 / Math.PI);
        var deltaLng = deltaLat / Math.Cos(latRad);
        return (
            (decimal)((double)centerLat - deltaLat),
            (decimal)((double)centerLat + deltaLat),
            (decimal)((double)centerLng - deltaLng),
            (decimal)((double)centerLng + deltaLng)
        );
    }

    private IQueryable<ServiceRequestEntity> BuildDiscoveryBaseQuery(
        long providerProfileId, ProviderServiceRequestDiscoveryFilter filter)
    {
        var biddableStatuses = new[] { ServiceRequestStatus.Open, ServiceRequestStatus.WaitingForOffer, ServiceRequestStatus.OfferReceived };

        var query = _db.ServiceRequests
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                && biddableStatuses.Contains(x.Status)
                && x.Assignment == null);

        // Offer state filter
        var offerState = filter.OfferState ?? OfferStateFilter.Any;
        if (offerState == OfferStateFilter.NotOffered)
            query = query.Where(x => !x.Offers.Any(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted));
        else if (offerState == OfferStateFilter.Offered)
            query = query.Where(x => x.Offers.Any(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted));

        // Category / location / priority / search filters
        if (!string.IsNullOrWhiteSpace(filter.ServiceCategoryCode))
            query = query.Where(x => x.ServiceCategoryCode == filter.ServiceCategoryCode.ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(filter.LocationCityCode))
            query = query.Where(x => x.LocationCityCode == filter.LocationCityCode);

        if (!string.IsNullOrWhiteSpace(filter.LocationCountryCode))
            query = query.Where(x => x.LocationCountryCode == filter.LocationCountryCode);

        if (filter.MinPriority.HasValue)
            query = query.Where(x => x.Priority >= filter.MinPriority.Value);

        // "New only": published after the caller last looked. Indexed on (Status, PublishedAt).
        if (filter.PublishedAfterUtc.HasValue)
            query = query.Where(x => x.PublishedAt != null && x.PublishedAt > filter.PublishedAfterUtc.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(term) ||
                x.RequestCode.ToLower().Contains(term));
        }

        return query;
    }

    private IQueryable<ServiceRequestEntity> BuildProviderOpenQuery(
        long providerProfileId, ProviderAvailableServiceRequestFilterRequest filter)
    {
        var biddableStatuses = new[] { ServiceRequestStatus.Open, ServiceRequestStatus.WaitingForOffer, ServiceRequestStatus.OfferReceived };

        var query = _db.ServiceRequests
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                && biddableStatuses.Contains(x.Status)
                && !x.Offers.Any(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted)
                && x.Assignment == null);

        if (!string.IsNullOrWhiteSpace(filter.ServiceCategoryCode))
            query = query.Where(x => x.ServiceCategoryCode == filter.ServiceCategoryCode.ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(filter.LocationCityCode))
            query = query.Where(x => x.LocationCityCode == filter.LocationCityCode);

        if (!string.IsNullOrWhiteSpace(filter.LocationCountryCode))
            query = query.Where(x => x.LocationCountryCode == filter.LocationCountryCode);

        if (filter.MinPriority.HasValue)
            query = query.Where(x => x.Priority >= filter.MinPriority.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(term) ||
                x.RequestCode.ToLower().Contains(term));
        }

        return query;
    }

    private IQueryable<ServiceRequestEntity> BuildAdminQuery(AdminServiceRequestFilterRequest filter)
    {
        var query = _db.ServiceRequests.AsNoTracking().Where(x => !x.IsDeleted);

        if (filter.VesselId.HasValue)
            query = query.Where(x => x.VesselId == filter.VesselId.Value);

        if (filter.OwnerUserId.HasValue)
            query = query.Where(x => x.OwnerUserId == filter.OwnerUserId.Value);

        if (filter.ProviderProfileId.HasValue)
            query = query.Where(x => x.Assignment != null && x.Assignment.ProviderProfileId == filter.ProviderProfileId.Value);

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        if (filter.Priority.HasValue)
            query = query.Where(x => x.Priority == filter.Priority.Value);

        if (filter.HasDispute.HasValue)
            query = query.Where(x => filter.HasDispute.Value ? x.Dispute != null : x.Dispute == null);

        if (!string.IsNullOrWhiteSpace(filter.ServiceCategoryCode))
            query = query.Where(x => x.ServiceCategoryCode == filter.ServiceCategoryCode.ToUpperInvariant());

        if (filter.CreatedFrom.HasValue)
            query = query.Where(x => x.CreateDate >= filter.CreatedFrom.Value);

        if (filter.CreatedTo.HasValue)
            query = query.Where(x => x.CreateDate <= filter.CreatedTo.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(term) ||
                x.RequestCode.ToLower().Contains(term));
        }

        return query;
    }
}
