using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Services;

/// <summary>
/// FIX_ASSIGNMENT_ON_ACCEPT — the single, shared create-assignment path. In the marketplace direct-accept flow the
/// accepted offer's provider IS the assignee, so the assignment is created automatically on owner accept; the manual
/// admin/provider create-assignment endpoint calls the same logic. One implementation, no drift.
///
/// Idempotent: if the SR already has an assignment (re-run / redelivery / a manual create after an auto-create), it
/// returns the existing one and does NOT double-assign. Sets SR → <see cref="ServiceRequestStatus.Assigned"/>,
/// <c>sr.SetAssignment</c>, adds the status-history entry, and publishes the AssignmentCreated realtime +
/// <see cref="ServiceRequestAssignmentCreatedMessage"/> — exactly as the manual handler did.
/// </summary>
public sealed class ServiceRequestAssignmentCreator
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IAizenMessagePublisher _messagePublisher;

    public ServiceRequestAssignmentCreator(
        IServiceRequestRepository srRepository,
        IServiceRequestAssignmentRepository assignmentRepository,
        ServiceRequestRealtimePublisher realtimePublisher,
        IAizenMessagePublisher messagePublisher)
    {
        _srRepository = srRepository;
        _assignmentRepository = assignmentRepository;
        _realtimePublisher = realtimePublisher;
        _messagePublisher = messagePublisher;
    }

    /// <summary>Result of a create attempt: the (existing or new) assignment, and whether it was created this call.</summary>
    public readonly record struct Result(ServiceRequestAssignmentEntity Assignment, bool Created);

    /// <summary>
    /// Get-or-create the assignment for the accepted offer's provider. Pass no schedule on the auto-accept path
    /// (<paramref name="scheduledStartDate"/>/<paramref name="scheduledEndDate"/>/<paramref name="assignedTeamMemberId"/>
    /// null — the provider sets the schedule when they start the job); the manual endpoint forwards its request values.
    /// </summary>
    public async Task<Result> CreateFromAcceptedOfferAsync(
        ServiceRequestEntity sr,
        ServiceRequestOfferEntity offer,
        long currentUserId,
        long? assignedTeamMemberId,
        DateTime? scheduledStartDate,
        DateTime? scheduledEndDate,
        CancellationToken ct)
    {
        // Idempotency: never create a second assignment for the same SR. On a re-run/redelivery the assignment already
        // exists — skip the create, but still ensure the SR rests in Assigned (a re-accept re-sets OfferAccepted just
        // before this, so left alone the SR would drift back off Assigned). No new history/publish on this path.
        var existing = sr.Assignment ?? await _assignmentRepository.GetByServiceRequestIdAsync(sr.Id, ct);
        if (existing is not null)
        {
            if (sr.Status != ServiceRequestStatus.Assigned)
            {
                sr.ChangeStatus(ServiceRequestStatus.Assigned);
                sr.SetAssignment(existing);
                _srRepository.Update(sr);
            }
            return new Result(existing, Created: false);
        }

        var assignment = ServiceRequestAssignmentEntity.Create(
            sr.Id, offer.Id, offer.ProviderProfileId, offer.ProviderUserId,
            assignedTeamMemberId, scheduledStartDate, scheduledEndDate);
        await _assignmentRepository.AddAsync(assignment, ct);

        var prevStatus = sr.Status;
        sr.ChangeStatus(ServiceRequestStatus.Assigned);
        sr.SetAssignment(assignment);
        sr.AddStatusHistory(ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.Assigned,
            "Assignment created", currentUserId, ServiceRequestActorType.Owner));
        _srRepository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, offer.ProviderProfileId,
            ServiceRequestRealtimeEventType.AssignmentCreated, assignment.ToDto(),
            currentUserId, ServiceRequestActorType.Owner, ct);

        await _messagePublisher.PublishAsync(new ServiceRequestAssignmentCreatedMessage
        {
            ServiceRequestId = sr.Id,
            AssignmentId = assignment.Id,
            ProviderProfileId = offer.ProviderProfileId,
            ProviderUserId = offer.ProviderUserId,
            ScheduledStartDate = scheduledStartDate,
        }, ct);

        return new Result(assignment, Created: true);
    }
}
