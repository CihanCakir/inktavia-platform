using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

/// <summary>
/// BE-I1 split-eligibility read: <see cref="IsSplitEligible"/> is the single gate signal (a created/verified sub-merchant
/// with a key). <see cref="Reason"/> is populated when not eligible (for UX/telemetry).
/// </summary>
public sealed class GetProviderSplitEligibilityRemoteCallResponse
{
    public required bool IsSplitEligible { get; init; }
    public required ProviderSubMerchantOnboardingStatus OnboardingStatus { get; init; }
    public bool     HasProfile { get; init; }
    public string?  Reason     { get; init; }
}
