namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// Whether a provider is actually IN the CargoDry programme.
///
/// PROV-MVP-002/G3 — CargoDry participation had no representation anywhere the provider-facing stack could read.
/// Onboarding records an INTEREST answer (<c>CargoDryInterest.interested</c>) plus a commercial-model preference,
/// but that is a stated wish, not an entitlement: a provider who ticked "yes" and has no agreement is not in the
/// programme, and one who ticked "no" and later signed one is. The authoritative record is an Active
/// <see cref="Domain.Entities.CargoDryConsignmentAgreementEntity"/> inside its date window.
///
/// The MarineProvider BFF reads this to publish a `CargoDry` capability on <c>GET /me/status</c>.
/// </summary>
public sealed class CargoDryProviderParticipationDto
{
    public long ProviderProfileId { get; init; }

    /// <summary>True when at least one consignment agreement is Active and valid right now.</summary>
    public bool IsParticipant { get; init; }

    /// <summary>How many. Diagnostic only — never a permission.</summary>
    public int ActiveAgreementCount { get; init; }

    public DateTime EvaluatedAtUtc { get; init; }
}
