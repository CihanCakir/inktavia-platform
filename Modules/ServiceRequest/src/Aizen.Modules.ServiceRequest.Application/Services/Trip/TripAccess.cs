using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Services.Trip;

/// <summary>Shared trip-command guard: tracking is allowed ONLY for the assigned provider on an accepted/active job.
/// Mirrors <c>StartServiceRequestAssignmentCommandHandler</c> — a foreign/unknown job yields a clean not-found so
/// existence never leaks.</summary>
public static class TripAccess
{
    // An "accepted job" is one that has been assigned and is not yet completed/closed — the window during which a
    // provider may be travelling to it.
    private static readonly ServiceRequestStatus[] Trackable =
    {
        ServiceRequestStatus.Assigned,
        ServiceRequestStatus.Scheduled,
        ServiceRequestStatus.InProgress,
    };

    public static void EnsureAssignedProviderOnTrackableJob(
        long providerProfileId, ServiceRequestEntity? sr, ServiceRequestAssignmentEntity? assignment)
    {
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        if (sr is null || assignment is null || assignment.ProviderProfileId != providerProfileId)
            throw new AizenBusinessException("Job not found.");   // never leak another provider's/foreign job
        if (!Trackable.Contains(sr.Status))
            throw new AizenBusinessException("SR_JOB_NOT_TRACKABLE");
    }
}
