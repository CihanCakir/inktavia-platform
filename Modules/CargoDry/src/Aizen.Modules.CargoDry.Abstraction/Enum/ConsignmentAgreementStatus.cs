namespace Aizen.Modules.CargoDry.Abstraction.Enum;

/// <summary>
/// Lifecycle status of a CargoDry consignment agreement.
/// Transitions: Draft → Active → Suspended → Terminated; Draft → Terminated; Active → Terminated.
/// Expired is set by a background job when EndDateUtc passes.
/// </summary>
public enum ConsignmentAgreementStatus
{
    Draft      = 1,
    Active     = 2,
    Suspended  = 3,
    Terminated = 4,
    Expired    = 5,
}
