using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

public sealed class CargoDryRenewalEntity : AizenEntity
{
    public long           KitId          { get; private set; }
    public long           OwnerUserId    { get; private set; }
    public DateTimeOffset RenewedAt      { get; private set; }
    public DateTimeOffset NewExpiresAt   { get; private set; }
    public int            AddedDays      { get; private set; }
    public RenewalType    RenewalType    { get; private set; }
    public string?        PaymentRef     { get; private set; }
    public long?          AdminUserId    { get; private set; }

    private CargoDryRenewalEntity() { }

    public static CargoDryRenewalEntity Create(
        long kitId, long ownerUserId, DateTimeOffset newExpiresAt,
        int addedDays, RenewalType type,
        string? paymentRef = null, long? adminUserId = null)
        => new()
        {
            KitId        = kitId,
            OwnerUserId  = ownerUserId,
            RenewedAt    = DateTimeOffset.UtcNow,
            NewExpiresAt = newExpiresAt,
            AddedDays    = addedDays,
            RenewalType  = type,
            PaymentRef   = paymentRef,
            AdminUserId  = adminUserId,
        };
}
