using Aizen.Bff.AdminPanel.Application.AdminProviders.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProviders.Query;

[DocumentationInfo("Get provider service requests BFF query",
    "Returns a paged list of service requests assigned to the given provider. " +
    "Routes through the ServiceRequest admin list endpoint with providerProfileId filter.")]
public sealed class GetProviderServiceRequestsBffQuery : AizenQuery<ProviderServiceRequestsBffResponse>
{
    public long ProviderProfileId { get; }
    public int  PageIndex         { get; }
    public int  PageSize          { get; }

    public GetProviderServiceRequestsBffQuery(long providerProfileId, int pageIndex = 0, int pageSize = 10)
    {
        ProviderProfileId = providerProfileId;
        PageIndex         = pageIndex;
        PageSize          = pageSize;
    }
}
