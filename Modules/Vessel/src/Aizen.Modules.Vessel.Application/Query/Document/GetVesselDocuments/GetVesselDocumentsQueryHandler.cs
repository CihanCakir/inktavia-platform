using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Repository.Persistence;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Application.Query.Document;

[DocumentationInfo("Get Vessel Documents Query Handler", "Returns a paged list of documents for a vessel; optionally enriches with pre-signed access URLs; cached for 15 minutes.")]
public sealed class GetVesselDocumentsQueryHandler : AizenQueryHandler<GetVesselDocumentsQuery, GetVesselDocumentsResponse>, IAizenQueryHandlerCacheable
{
    private readonly IAizenUnitOfWork<VesselDbContext> _uow;
    private readonly IVesselFileStorageService _fileStorage;
    private readonly IAizenInfoAccessor _info;

    public GetVesselDocumentsQueryHandler(
        IAizenUnitOfWork<VesselDbContext> uow,
        IVesselFileStorageService fileStorage,
        IAizenInfoAccessor info)
    {
        _uow = uow;
        _fileStorage = fileStorage;
        _info = info;
    }

    public override async Task<GetVesselDocumentsResponse?> Handle(GetVesselDocumentsQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<VesselDocumentEntity>();

        var result = await repo.GetPagedListAsync<VesselDocumentDto>(
            selector: d => new VesselDocumentDto
            {
                Id = d.Id,
                VesselId = d.VesselId,
                DocumentTypeCode = d.DocumentTypeCode,
                DocumentName = d.DocumentName,
                FileId = d.FileId,
                OriginalFileNameSnapshot = d.OriginalFileNameSnapshot,
                ContentTypeSnapshot = d.ContentTypeSnapshot,
                SizeInBytesSnapshot = d.SizeInBytesSnapshot,
                ExpiresAt = d.ExpiresAt,
                Status = d.DocumentStatus,
                Notes = d.Notes,
                IsActive = d.IsActive
            },
            predicate: d => d.VesselId == request.VesselId,
            orderBy: q => q.OrderByDescending(d => d.Id),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        if (request.IncludeAccessUrls)
        {
            var accessToken = _info.UserInfoAccessor.UserInfo.AccessToken;
            var expiresIn = TimeSpan.FromMinutes(request.AccessUrlExpiresInMinutes > 0 ? request.AccessUrlExpiresInMinutes : 15);
            const int chunkSize = 10;
            var chunks = result.Items.Where(d => d.FileId.HasValue).Chunk(chunkSize);
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

        return new GetVesselDocumentsResponse((Paginate<VesselDocumentDto>)result);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
