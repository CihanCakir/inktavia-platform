namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

/// <summary>BE-I1 — asks Payment whether a provider has a split-eligible sub-merchant (P9 prerequisite gate).</summary>
public sealed class GetProviderSplitEligibilityRemoteCallRequest
{
    public required long ProviderProfileId { get; init; }
}
