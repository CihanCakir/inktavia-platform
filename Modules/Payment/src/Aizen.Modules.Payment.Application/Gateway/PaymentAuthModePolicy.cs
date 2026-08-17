using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Application.Gateway;

/// <summary>
/// BE-P9 §1 — admin-configurable auth-mode policy (bound from the "Payment:AuthMode" section; NOT hardcoded). Default
/// Capture with an optional per-category override map. A config-backed policy (no table) is sufficient for the MVP;
/// promote to a versioned rule entity (mirroring PlatformFeeRule/ProfitProtectionPolicy) if per-tenant scheduling is needed.
/// </summary>
public sealed class PaymentAuthModeOptions
{
    public const string SectionName = "Payment:AuthMode";

    /// <summary>Platform default. Capture unless overridden.</summary>
    public PaymentAuthMode Default { get; set; } = PaymentAuthMode.Capture;

    /// <summary>Per-category overrides, keyed by ServiceCategoryCode (case-insensitive at resolve).</summary>
    public Dictionary<string, PaymentAuthMode> CategoryOverrides { get; set; } = new();
}

/// <summary>Pure resolver: category override wins over the default. Unit-tested.</summary>
public static class PaymentAuthModeResolver
{
    public static PaymentAuthMode Resolve(string? categoryCode, PaymentAuthModeOptions options)
    {
        if (!string.IsNullOrWhiteSpace(categoryCode) && options.CategoryOverrides is { Count: > 0 })
            foreach (var kv in options.CategoryOverrides)
                if (string.Equals(kv.Key, categoryCode, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;

        return options.Default;
    }
}
