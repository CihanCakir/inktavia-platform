namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>How a customer discount is computed (§19.6). Stable int (HasConversion&lt;int&gt;).</summary>
public enum CustomerDiscountType
{
    Percent = 1,   // Round(base × DiscountRate)
    Fixed   = 2,   // FixedDiscountAmount
}
