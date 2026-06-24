using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.WorkLog;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Command.WorkLog;

[DocumentationInfo("Add work log entry command handler", "Persists a work log entry and publishes WorkLogAdded realtime event.")]
public sealed class AddWorkLogEntryCommandHandler : AizenCommandHandler<AddWorkLogEntryCommand, AddWorkLogEntryResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestWorkLogRepository _workLogRepository;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public AddWorkLogEntryCommandHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestWorkLogRepository workLogRepository,
        ServiceRequestRealtimePublisher realtimePublisher)
    {
        _srRepository = srRepository;
        _workLogRepository = workLogRepository;
        _realtimePublisher = realtimePublisher;
    }

    public override async Task<AddWorkLogEntryResponse?> Handle(AddWorkLogEntryCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var req = request.Request;
        var authorId = req.AuthorId ?? 0;

        var workLog = ServiceRequestWorkLogEntity.Create(
            sr.Id,
            sr.Assignment?.Id ?? 0,
            authorId,
            ServiceRequestWorkLogType.GeneralNote,
            req.Content,
            req.Content,
            null, null,
            string.IsNullOrWhiteSpace(req.MediaFileId) ? null : Guid.TryParse(req.MediaFileId, out var gid) ? gid : null);

        await _workLogRepository.AddAsync(workLog, cancellationToken);

        var entryDto = new WorkLogEntryItemDto
        {
            Id = workLog.Id.ToString(),
            Timestamp = workLog.LoggedAt != default ? new DateTimeOffset(workLog.LoggedAt, TimeSpan.Zero) : DateTimeOffset.UtcNow,
            Type = req.Type,
            Content = req.Content,
            MediaUrl = null,
            Author = req.Author
        };

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, sr.Assignment?.ProviderProfileId,
            ServiceRequestRealtimeEventType.WorkLogAdded, entryDto, authorId, ServiceRequestActorType.Provider, cancellationToken);

        return new AddWorkLogEntryResponse(entryDto);
    }
}
