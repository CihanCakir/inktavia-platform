using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class GetMyOffersBffQuery : AizenQuery<GetMyOffersResponse>
{
    public int PageIndex { get; init; }
    public int PageSize { get; init; } = 20;
    public int? Status { get; init; }
}
