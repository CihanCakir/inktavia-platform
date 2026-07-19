using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Query.WorkLog;

[DocumentationInfo("Get work logs query handler", "Returns all work logs for the given assignment, ownership-checked.")]
public sealed class GetServiceRequestWorkLogsQueryHandler : AizenQueryHandler<GetServiceRequestWorkLogsQuery, GetServiceRequestWorkLogsResponse>
{
    private readonly IServiceRequestWorkLogRepository _repository;
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IAizenInfoAccessor _info;

    public GetServiceRequestWorkLogsQueryHandler(
        IServiceRequestWorkLogRepository repository,
        IServiceRequestAssignmentRepository assignmentRepository,
        IAizenInfoAccessor info)
    {
        _repository = repository;
        _assignmentRepository = assignmentRepository;
        _info = info;
    }

    public override async Task<GetServiceRequestWorkLogsResponse> Handle(GetServiceRequestWorkLogsQuery request, CancellationToken cancellationToken)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken)
            ?? throw new AizenBusinessException("Job not found.");
        if (assignment.ProviderProfileId != providerProfileId)
            throw new AizenBusinessException("Job not found.");

        var logs = await _repository.GetByAssignmentIdAsync(request.AssignmentId, cancellationToken);
        return new GetServiceRequestWorkLogsResponse(logs.Select(l => l.ToDto()).ToList());
    }
}
