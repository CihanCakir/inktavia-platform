using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;

namespace Aizen.Modules.Vessel.Application.Query.Document;

[DocumentationInfo("Get Vessel Documents Query Handler", "Returns all documents for a vessel; cached for 15 minutes.")]
public sealed class GetVesselDocumentsQueryHandler : AizenQueryHandler<GetVesselDocumentsQuery, IReadOnlyList<VesselDocumentDto>>, IAizenQueryHandlerCacheable
{
    private readonly IVesselDocumentRepository _documentRepository;

    public GetVesselDocumentsQueryHandler(IVesselDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public override async Task<IReadOnlyList<VesselDocumentDto>> Handle(GetVesselDocumentsQuery request, CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.GetByVesselIdAsync(request.VesselId, cancellationToken);
        return documents.Select(d => d.ToDto()).ToList().AsReadOnly();
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
}
