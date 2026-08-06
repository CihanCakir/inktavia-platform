namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("Maintenance schedule DTO", "A per-vessel, per-category recurring maintenance schedule (S12).")]
public sealed class MaintenanceScheduleDto
{
    public long Id { get; set; }
    public long VesselId { get; set; }
    public long OwnerUserId { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public string? ServiceTypeCode { get; set; }
    public int RecommendedIntervalMonths { get; set; }
    public int ReminderLeadDays { get; set; }
    public DateTime? LastPerformedAt { get; set; }
    public DateTime NextDueAt { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}
