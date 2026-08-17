namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Outcome of a profit-protection evaluation (§19.11). Integer values are stable (HasConversion&lt;int&gt;).
/// </summary>
public enum ProfitProtectionDecisionState
{
    /// <summary>All requested advantages are safe — every contribution gate passes as requested.</summary>
    Approved               = 1,

    /// <summary>A requested advantage was unsafe → reduced (per AdjustmentOrder) to the safe maximum; gates then pass.</summary>
    ApprovedWithAdjustment = 2,

    /// <summary>No safe combination (even at zero advantage) meets the minimum contributions → payment must not start.</summary>
    Rejected               = 3,

    /// <summary>Missing/conflicting policy or uncomputable funding → advantage not applied + ops alarm.</summary>
    ConfigurationError     = 4,
}
