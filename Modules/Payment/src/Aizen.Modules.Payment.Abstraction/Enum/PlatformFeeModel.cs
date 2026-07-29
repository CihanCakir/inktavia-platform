namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// How a platform fee is computed on the base amount (§1, §2.3, BE-P3).
/// Integer values are stable (stored via HasConversion&lt;int&gt;).
/// </summary>
public enum PlatformFeeModel
{
    /// <summary>feeNet = Round(base × Rate).</summary>
    Percentage           = 1,

    /// <summary>feeNet = FixedAmount (base-independent).</summary>
    Fixed                = 2,

    /// <summary>feeNet = Clamp(Round(base × Rate), MinAmount, MaxAmount).</summary>
    PercentageWithBounds = 3,

    /// <summary>feeNet = 0 (no platform fee).</summary>
    Waived               = 4,
}
