using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Query.ServiceRequest;

[DocumentationInfo("Get service request detail query handler", "Fetches a service request with all related data and maps to detail DTO.")]
public sealed class GetServiceRequestDetailQueryHandler : AizenQueryHandler<GetServiceRequestDetailQuery, GetServiceRequestDetailResponse>
{
    private readonly IServiceRequestRepository _repository;

    public GetServiceRequestDetailQueryHandler(IServiceRequestRepository repository) => _repository = repository;

    public override async Task<GetServiceRequestDetailResponse> Handle(GetServiceRequestDetailQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdWithDetailsAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        return new GetServiceRequestDetailResponse(entity.ToDetailDto());
    }
}
