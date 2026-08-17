namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Identifies the scope of a commission rule.
/// Resolution precedence: ProviderOverride > Plan > Category > Global.
/// </summary>
public enum CommissionRuleType
{
    Global           = 1,  // Platform-wide default
    Category         = 2,  // Per service category code
    Plan             = 3,  // Per provider plan
    ProviderOverride = 4,  // Individual provider negotiated rate
}
