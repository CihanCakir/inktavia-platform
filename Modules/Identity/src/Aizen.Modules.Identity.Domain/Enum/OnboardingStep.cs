namespace Aizen.Modules.Identity.Domain.Enum;

public enum OnboardingStep
{
    BusinessIdentity,
    ServiceCapabilities,
    OperatingRegion,
    ComplianceVerification,
    CargoDryInterest,
    ReviewSubmit
}

public enum OnboardingStepStatus
{
    NotStarted,
    InProgress,
    Completed,
    NeedsRevision,
    Blocked
}
