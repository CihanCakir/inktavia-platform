using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get All Vessels Admin Query Handler", "Returns a paged list of all vessels for admin use; cached for 5 minutes.")]
public sealed class GetAllVesselsAdminQueryHandler : AizenQueryHandler<GetAllVesselsAdminQuery, GetAllVesselsAdminResponse>, IAizenQueryHandlerCacheable
{
    private readonly IAizenUnitOfWork<VesselDbContext> _uow;

    public GetAllVesselsAdminQueryHandler(IAizenUnitOfWork<VesselDbContext> uow)
    {
        _uow = uow;
    }



    public override async Task<GetAllVesselsAdminResponse?> Handle(GetAllVesselsAdminQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<VesselEntity>();

        // Normalize filter arrays: remove 0 values sent by the UI as "no filter" placeholders.
        var assetTypes = request.AssetTypes?.Where(x => x != 0).ToArray();
        var operationalStatuses = request.OperationalStatuses?.Where(x => x != 0).ToArray();

        var result = await repo.GetPagedListAsync<VesselListItemDto>(
            selector: v => new VesselListItemDto
            {
                Id = v.Id,
                PublicId = v.PublicId,
                VesselCode = v.VesselCode,
                Name = v.Name,
                Slug = v.Slug,
                VesselTypeCode = v.VesselTypeCode,
                FlagCountryCode = v.FlagCountryCode,
                CoverMediaUrl = v.Media
                    .Where(m => m.IsCover && m.IsActive && !m.IsDeleted)
                    .Select(m => m.ThumbnailUrl)
                    .FirstOrDefault(),
                Status = v.Status,
                Visibility = v.Visibility,
                IsArchived = v.IsArchived,
                CreateDate = v.CreateDate,
                OperationalStatus = v.OperationalStatus,
                AssetType = v.AssetType,
                LengthMeters = v.Specification != null ? v.Specification.LengthValue : null,
                GrossTonnage = v.Specification != null ? v.Specification.GrossTonnage : null,

                // Primary owner identity — enriched by BFF; vessel module provides IDs only.
                OwnerUserId = v.Owners
                    .Where(o => o.IsPrimary && o.IsActive)
                    .Select(o => (long?)o.UserId)
                    .FirstOrDefault(),
                OwnerProfileId = v.Owners
                    .Where(o => o.IsPrimary && o.IsActive)
                    .Select(o => o.UserProfileId)
                    .FirstOrDefault(),
                OwnershipStatus = (int?)v.Owners
                    .Where(o => o.IsPrimary && o.IsActive)
                    .Select(o => (int?)o.OwnershipStatus)
                    .FirstOrDefault(),

                // Latest current location snapshot.
                Latitude = (double?)v.LocationSnapshots
                    .Where(l => l.IsCurrent && l.IsActive)
                    .OrderByDescending(l => l.CapturedAt)
                    .Select(l => l.Latitude)
                    .FirstOrDefault(),
                Longitude = (double?)v.LocationSnapshots
                    .Where(l => l.IsCurrent && l.IsActive)
                    .OrderByDescending(l => l.CapturedAt)
                    .Select(l => l.Longitude)
                    .FirstOrDefault(),
                LastPositionDate = v.LocationSnapshots
                    .Where(l => l.IsCurrent && l.IsActive)
                    .OrderByDescending(l => l.CapturedAt)
                    .Select(l => (DateTime?)l.CapturedAt)
                    .FirstOrDefault(),
                LastLocationMarinaName = v.LocationSnapshots
                    .Where(l => l.IsCurrent && l.IsActive)
                    .OrderByDescending(l => l.CapturedAt)
                    .Select(l => l.MarinaName)
                    .FirstOrDefault()
            },
            predicate: v =>
                (request.IsArchived == null || v.IsArchived == request.IsArchived) &&
                (request.SearchTerm == null || v.Name.Contains(request.SearchTerm) || v.VesselCode.Contains(request.SearchTerm)) &&
                (assetTypes == null || assetTypes.Length == 0 || (v.AssetType != null && assetTypes.Contains((int)v.AssetType))) &&
                (operationalStatuses == null || operationalStatuses.Length == 0 || (v.OperationalStatus != null && operationalStatuses.Contains((int)v.OperationalStatus))) &&
                (request.OwnerUserId == null || v.Owners.Any(o => o.UserId == request.OwnerUserId && o.IsActive)),
            orderBy: q => q.OrderByDescending(v => v.CreateDate),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        return new GetAllVesselsAdminResponse((Paginate<VesselListItemDto>)result);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
}
