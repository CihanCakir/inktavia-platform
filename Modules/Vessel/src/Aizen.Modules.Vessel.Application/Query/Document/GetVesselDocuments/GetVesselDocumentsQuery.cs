using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Document;

namespace Aizen.Modules.Vessel.Application.Query.Document;

public sealed class GetVesselDocumentsQuery : AizenPagedQuery<VesselDocumentDto>
{
    public long VesselId { get; }
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetVesselDocumentsQuery(long vesselId, int pageIndex = 0, int pageSize = 20)
    {
        VesselId = vesselId;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
