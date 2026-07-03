namespace Aizen.Modules.CargoDry.Abstraction.Enum;

public enum CargoDryKitStatus
{
    Available   = 1,
    Activated   = 2,
    Expired     = 3,
    Renewed     = 4,
    Revoked     = 5,
    Lost        = 6,
    Transferred = 7,

    /// <summary>
    /// Kit was activated but has no recorded sale attribution (SalesChannel not set).
    /// No invoice or payout is generated until admin resolves the commercial attribution.
    /// Decision N18/N19 (July 2026).
    /// </summary>
    CommercialReviewRequired = 8,
}
