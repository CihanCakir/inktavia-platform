using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest work log type enum", "Type of work log entry recorded during assignment execution.")]
public enum ServiceRequestWorkLogType
{
    GeneralNote = 1,
    ArrivedAtVessel = 2,
    InspectionStarted = 3,
    InspectionCompleted = 4,
    WorkStarted = 5,
    MaterialRequired = 6,
    AdditionalIssueFound = 7,
    WaitingForOwnerApproval = 8,
    WorkPaused = 9,
    WorkResumed = 10,
    WorkCompleted = 11
}
