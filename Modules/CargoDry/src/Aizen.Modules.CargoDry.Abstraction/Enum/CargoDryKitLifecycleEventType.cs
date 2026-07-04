namespace Aizen.Modules.CargoDry.Abstraction.Enum;

/// <summary>
/// Defines the type of a kit lifecycle event recorded in <c>CargoDryKitLifecycleEventEntity</c>.
/// Append-only — do not remove or renumber values.
/// </summary>
public enum CargoDryKitLifecycleEventType
{
    Created                  = 0,
    Activated                = 1,
    Renewed                  = 2,
    Extended                 = 3,
    Transferred              = 4,
    Revoked                  = 5,
    Expired                  = 6,
    Validated                = 7,
    CommercialReviewRequired = 8,
    ManualAdjustment         = 9,
}
