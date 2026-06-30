using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.WorkLog;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.WorkLog;

[DocumentationInfo("Add work log command handler", "Persists a work log entry and publishes WorkLogAdded realtime event.")]
public sealed class AddServiceRequestWorkLogCommandHandler : AizenCommandHandler<AddServiceRequestWorkLogCommand, AddServiceRequestWorkLogResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IServiceRequestWorkLogRepository _workLogRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public AddServiceRequestWorkLogCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestAssignmentRepository assignmentRepository,
        IServiceRequestWorkLogRepository workLogRepository, IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher)
    {
        _srRepository = srRepository; _assignmentRepository = assignmentRepository;
        _workLogRepository = workLogRepository; _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<AddServiceRequestWorkLogResponse?> Handle(AddServiceRequestWorkLogCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Assignment {request.AssignmentId} not found.");
        var sr = await _srRepository.GetByIdAsync(assignment.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {assignment.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var req = request.Request;

        var workLog = ServiceRequestWorkLogEntity.Create(
            sr.Id, assignment.Id, currentUserId, req.LogType, req.Title,
            req.Description, req.LocationLatitude, req.LocationLongitude, req.AttachmentFileId);

        await _workLogRepository.AddAsync(workLog, cancellationToken);
        assignment.AddWorkLog(workLog);
        _assignmentRepository.Update(assignment);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, assignment.ProviderProfileId,
            ServiceRequestRealtimeEventType.WorkLogAdded, workLog.ToDto(),
            currentUserId, ServiceRequestActorType.Provider, cancellationToken);

        return new AddServiceRequestWorkLogResponse(workLog.ToDto());
    }
}
