using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Payment.Abstraction.Message;

/// <summary>
/// Requests ASYNC iyzico sub-merchant provisioning for a provider payment profile. Published on data-submit, by the
/// hourly retry sweep, and by the admin "approve billing" action. PII-FREE by design: carries only the provider
/// profile id + attempt metadata; the KYC needed for the gateway call is read (encrypted) from the profile by the
/// consumer, never from the bus.
/// </summary>
public sealed class ProviderSubMerchantProvisioningRequested : AizenBaseMessage
{
    public long ProviderProfileId { get; init; }
    /// <summary>The profile's failed-attempt count at publish time (metadata / diagnostics).</summary>
    public int  AttemptNumber     { get; init; }
    /// <summary>What triggered this: "submit" | "retry-sweep" | "admin".</summary>
    public string? Source         { get; init; }
}
