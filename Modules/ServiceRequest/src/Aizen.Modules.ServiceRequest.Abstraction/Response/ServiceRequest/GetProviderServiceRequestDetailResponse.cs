using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

public sealed class GetProviderServiceRequestDetailResponse
{
    public ProviderServiceRequestDetailDto Detail { get; init; } = default!;

    public GetProviderServiceRequestDetailResponse() { }
    public GetProviderServiceRequestDetailResponse(ProviderServiceRequestDetailDto detail) => Detail = detail;
}
