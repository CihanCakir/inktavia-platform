using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Application.Query.Document;

[DocumentationInfo("Get Vessel Documents Query Handler", "Returns a paged list of documents for a vessel; cached for 15 minutes.")]
public sealed class GetVesselDocumentsQueryHandler : AizenQueryHandler<GetVesselDocumentsQuery, IPaginate<VesselDocumentDto>>, IAizenQueryHandlerCacheable
{
    private readonly IAizenUnitOfWork<VesselDbContext> _uow;

    public GetVesselDocumentsQueryHandler(IAizenUnitOfWork<VesselDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<IPaginate<VesselDocumentDto>> Handle(GetVesselDocumentsQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<VesselDocumentEntity>();

        return await repo.GetPagedListAsync<VesselDocumentDto>(
            selector: d => new VesselDocumentDto
            {
                Id = d.Id,
                VesselId = d.VesselId,
                DocumentTypeCode = d.DocumentTypeCode,
                DocumentName = d.DocumentName,
                FileId = d.FileId,
                FileName = d.FileName,
                FileUrl = d.FileUrl,
                MimeType = d.MimeType,
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
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
