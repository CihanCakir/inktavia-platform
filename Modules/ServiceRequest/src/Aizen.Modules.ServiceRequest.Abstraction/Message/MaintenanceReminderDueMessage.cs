using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

/// <summary>
/// N2 — published by the daily <c>MaintenanceReminderDueJob</c> when a maintenance schedule enters its reminder
/// window (now ≥ NextDueAt − ReminderLeadDays) and hasn't been reminded this cycle. The Notification module consumes
/// it and reminds the owner through the N-B path. One message per cycle (idempotency guarded by the schedule's
/// <c>ReminderSentAt</c> marker).
/// </summary>
[DocumentationInfo("Maintenance reminder due message", "A vessel maintenance schedule is due soon; remind the owner (N2).")]
public sealed class MaintenanceReminderDueMessage : AizenBaseMessage
{
    public long ScheduleId { get; set; }
    public long OwnerUserId { get; set; }
    public long VesselId { get; set; }
    public string? VesselName { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public string? ServiceTypeCode { get; set; }
    public DateTime NextDueAt { get; set; }
}
