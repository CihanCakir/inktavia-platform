namespace Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;

public sealed class UpdateWorkPhaseRequest
{
    public int ProgressPercent { get; set; }
    /// <summary>Upcoming | In Progress | Completed</summary>
    public string Status { get; set; } = string.Empty;
}
