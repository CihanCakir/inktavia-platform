using Aizen.Core.Domain;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Owner Preferred Provider entity",
    "Owner-level (additive, CargoDry supply flow): the provider recorded as an owner's preferred CargoDry supplier on " +
    "their FIRST completed CARGODRY_SUPPLY service request. Set-once (idempotent) — later completions do not overwrite. " +
    "Effect (v1): the preferred provider sees the owner's subsequent CARGODRY_SUPPLY requests flagged/sorted first " +
    "(isPreferred). No exclusivity — other program providers still see them.")]
public sealed class CargoDryOwnerPreferredProviderEntity : AizenEntityWithAudit
{
    /// <summary>Boat owner user id (owner-level key — one preferred provider per owner).</summary>
    public long OwnerUserId { get; private set; }

    /// <summary>The preferred provider's profile id.</summary>
    public long ProviderProfileId { get; private set; }

    /// <summary>The first completed CARGODRY_SUPPLY SR that established this preference (audit).</summary>
    public long FirstServiceRequestId { get; private set; }

    public DateTime SetAtUtc { get; private set; }

    private CargoDryOwnerPreferredProviderEntity() { }

    public static CargoDryOwnerPreferredProviderEntity Create(
        long ownerUserId, long providerProfileId, long firstServiceRequestId, DateTime nowUtc)
        => new()
        {
            OwnerUserId           = ownerUserId,
            ProviderProfileId     = providerProfileId,
            FirstServiceRequestId = firstServiceRequestId,
            SetAtUtc              = nowUtc,
            IsActive              = true,
        };
}
