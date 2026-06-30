using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Query.WorkLog;

[DocumentationInfo("Get work logs query handler", "Returns all work logs for the given assignment.")]
public sealed class GetServiceRequestWorkLogsQueryHandler : AizenQueryHandler<GetServiceRequestWorkLogsQuery, GetServiceRequestWorkLogsResponse>
{
    private readonly IServiceRequestWorkLogRepository _repository;

    public GetServiceRequestWorkLogsQueryHandler(IServiceRequestWorkLogRepository repository) => _repository = repository;

    public override async Task<GetServiceRequestWorkLogsResponse> Handle(GetServiceRequestWorkLogsQuery request, CancellationToken cancellationToken)
    {
        var logs = await _repository.GetByAssignmentIdAsync(request.AssignmentId, cancellationToken);
        return new GetServiceRequestWorkLogsResponse(logs.Select(l => l.ToDto()).ToList());
    }
}
