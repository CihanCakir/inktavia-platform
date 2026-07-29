namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// BE-P9 — iyzico authorization mode (§21.3–21.4). <b>Default = Capture</b>: funds are captured into the protected pool at
/// acceptance and released to the sub-merchant by an approve after completion (matches the marine ~7-day window, well inside
/// iyzico's 25-day PostAuth ceiling). <b>PreAuth</b> is optional per policy: funds are reserved and PostAuth'd within the
/// 25-day BKM ceiling (PostAuth job = documented follow-up).
/// </summary>
public enum PaymentAuthMode
{
    Capture = 1,
    PreAuth = 2,
}
