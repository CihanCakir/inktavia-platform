using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetMyOffers;

[DocumentationInfo("Get my offers query", "Returns all offers made by the calling provider, filterable by status.")]
public sealed class GetMyOffersQuery : AizenQuery<GetMyOffersResponse>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public ServiceRequestOfferStatus? StatusFilter { get; init; }

    public GetMyOffersQuery(int pageIndex = 0, int pageSize = 20)
    {
        PageIndex = pageIndex < 0 ? 0 : pageIndex;
        PageSize = pageSize is <= 0 or > 100 ? 20 : pageSize;
    }
}
