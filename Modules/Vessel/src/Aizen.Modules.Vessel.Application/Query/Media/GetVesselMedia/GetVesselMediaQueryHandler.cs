using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Repository.Persistence;
using Aizen.Modules.Vessel.Abstraction.Response.Media;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Application.Query.Media;

[DocumentationInfo("Get Vessel Media Query Handler", "Returns a paged list of media items for a vessel ordered by sort order; optionally enriches with pre-signed access URLs; cached for 15 minutes.")]
public sealed class GetVesselMediaQueryHandler : AizenQueryHandler<GetVesselMediaQuery, GetVesselMediaResponse>, IAizenQueryHandlerCacheable
{
    private readonly IAizenUnitOfWork<VesselDbContext> _uow;
    private readonly IVesselFileStorageService _fileStorage;
    private readonly IAizenInfoAccessor _info;

    public GetVesselMediaQueryHandler(
        IAizenUnitOfWork<VesselDbContext> uow,
        IVesselFileStorageService fileStorage,
        IAizenInfoAccessor info)
    {
        _uow = uow;
        _fileStorage = fileStorage;
        _info = info;
    }

    public override async Task<GetVesselMediaResponse?> Handle(GetVesselMediaQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<VesselMediaEntity>();

        var result = await repo.GetPagedListAsync<VesselMediaDto>(
            selector: m => new VesselMediaDto
            {
                Id = m.Id,
                VesselId = m.VesselId,
                MediaType = m.MediaType,
                FileId = m.FileId,
                OriginalFileNameSnapshot = m.OriginalFileNameSnapshot,
                ContentTypeSnapshot = m.ContentTypeSnapshot,
                SizeInBytesSnapshot = m.SizeInBytesSnapshot,
                SortOrder = m.SortOrder,
                IsCover = m.IsCover,
                IsActive = m.IsActive
            },
            predicate: m => m.VesselId == request.VesselId,
            orderBy: q => q.OrderBy(m => m.SortOrder),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        if (request.IncludeAccessUrls)
        {
            var accessToken = _info.UserInfoAccessor.UserInfo.AccessToken;
            var expiresIn = TimeSpan.FromMinutes(request.AccessUrlExpiresInMinutes > 0 ? request.AccessUrlExpiresInMinutes : 15);
            const int chunkSize = 10;
            var chunks = result.Items.Where(m => m.FileId.HasValue).Chunk(chunkSize);
            foreach (var chunk in chunks)
            {
                await Task.WhenAll(chunk.Select(async dto =>
                {
                    var urlResult = await _fileStorage.CreateReadUrlAsync(dto.FileId!.Value, expiresIn, accessToken, cancellationToken);
                    if (urlResult is not null)
                    {
                        dto.AccessUrl = urlResult.ReadUrl;
                        dto.AccessUrlExpiresAt = urlResult.ExpiresAt;
                    }
                }));
            }
        }

        return new GetVesselMediaResponse((Paginate<VesselMediaDto>)result);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
