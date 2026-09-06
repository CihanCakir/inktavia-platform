using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Repository.Persistence;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get User Vessels Query Handler", "Returns a paged list of vessels owned by the user; cover populated from the vessel's cover photo as a pre-signed URL; cached for 10 minutes.")]
public sealed class GetUserVesselsQueryHandler : AizenQueryHandler<GetUserVesselsQuery, GetUserVesselsResponse>, IAizenQueryHandlerCacheable
{
    // Resolve the cover read URL with a TTL comfortably above the 10-min list cache so a cached list never serves
    // an expired cover URL.
    private static readonly TimeSpan CoverUrlTtl = TimeSpan.FromMinutes(30);

    private readonly IAizenUnitOfWork<VesselDbContext> _uow;
    private readonly IVesselFileStorageService _fileStorage;
    private readonly IAizenInfoAccessor _info;

    public GetUserVesselsQueryHandler(
        IAizenUnitOfWork<VesselDbContext> uow,
        IVesselFileStorageService fileStorage,
        IAizenInfoAccessor info)
    {
        _uow = uow;
        _fileStorage = fileStorage;
        _info = info;
    }

    public override async Task<GetUserVesselsResponse?> Handle(GetUserVesselsQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<VesselOwnerEntity>();

        var result = await repo.GetPagedListAsync<VesselListItemDto>(
            selector: o => new VesselListItemDto
            {
                Id = o.Vessel!.Id,
                PublicId = o.Vessel.PublicId,
                VesselCode = o.Vessel.VesselCode,
                Name = o.Vessel.Name,
                Slug = o.Vessel.Slug,
                VesselTypeCode = o.Vessel.VesselTypeCode,
                FlagCountryCode = o.Vessel.FlagCountryCode,
                CoverMediaUrl = null,
                // Length lives on the spec — the list projection previously dropped it, so Home/list showed "—"/"0m"
                // even though it persists (detail returns it). Populate it here (LEFT JOIN via the null-conditional).
                LengthMeters = o.Vessel.Specification != null ? o.Vessel.Specification.LengthValue : null,
                Status = o.Vessel.Status,
                Visibility = o.Vessel.Visibility,
                IsArchived = o.Vessel.IsArchived,
                CreateDate = o.Vessel.CreateDate,
                // Owner-chosen location for the list card. Projected unconditionally (EF-safe); consumers treat
                // an all-null selection as "not set" (mirrors ToSelectedLocationDto's presence rule).
                SelectedLocation = new Aizen.Modules.Vessel.Abstraction.Dto.Location.VesselSelectedLocationDto
                {
                    MarinaId = o.Vessel.SelectedLocationMarinaId,
                    MarinaName = o.Vessel.SelectedLocationMarinaName,
                    CustomLabel = o.Vessel.SelectedLocationCustomLabel,
                    Latitude = o.Vessel.SelectedLocationLatitude,
                    Longitude = o.Vessel.SelectedLocationLongitude,
                    SetAt = o.Vessel.SelectedLocationSetAt
                }
            },
            predicate: o => o.UserId == request.UserId && o.OwnershipStatus == VesselOwnershipStatus.Active,
            orderBy: q => q.OrderByDescending(o => o.Vessel!.CreateDate),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        var paged = (Paginate<VesselListItemDto>)result;
        // Items are materialized reference types — mutating them updates what `paged` returns.
        await PopulateCoverUrlsAsync(paged.Items.ToList(), cancellationToken);

        return new GetUserVesselsResponse(paged);
    }

    // Populate CoverMediaUrl from each vessel's active cover photo, resolved to a pre-signed read URL. Kept as a
    // separate step (the presigned URL cannot be produced inside the EF projection) — a small, module-consistent
    // enrichment (the field was hardcoded null), so the list / Home card / picker render a real cover.
    private async Task PopulateCoverUrlsAsync(IList<VesselListItemDto> items, CancellationToken ct)
    {
        if (items is null || items.Count == 0) return;

        var vesselIds = items.Select(i => i.Id).ToList();
        var mediaRepo = _uow.GetRepository<VesselMediaEntity>();
        var covers = await mediaRepo.GetPagedListAsync<CoverProjection>(
            selector: m => new CoverProjection { VesselId = m.VesselId, FileId = m.FileId },
            predicate: m => vesselIds.Contains(m.VesselId) && m.IsCover && m.IsActive && m.FileId != null,
            orderBy: q => q.OrderBy(m => m.VesselId),
            pageIndex: 0,
            pageSize: vesselIds.Count,
            cancellationToken: ct);

        var coverByVessel = ((Paginate<CoverProjection>)covers).Items
            .Where(c => c.FileId.HasValue)
            .GroupBy(c => c.VesselId)
            .ToDictionary(g => g.Key, g => g.First().FileId!.Value);
        if (coverByVessel.Count == 0) return;

        var accessToken = _info.UserInfoAccessor.UserInfo.AccessToken;
        foreach (var item in items)
        {
            if (!coverByVessel.TryGetValue(item.Id, out var fileId)) continue;
            var url = await _fileStorage.CreateReadUrlAsync(fileId, CoverUrlTtl, accessToken, ct);
            if (url is not null) item.CoverMediaUrl = url.ReadUrl;
        }
    }

    private sealed class CoverProjection
    {
        public long VesselId { get; set; }
        public Guid? FileId { get; set; }
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
}
