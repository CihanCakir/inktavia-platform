using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Model;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Kit entity",
    "The digital twin of a physical moisture protection kit. " +
    "Status transitions are enforced through domain methods only. " +
    "EfficiencyPercent and DaysUntilExpiry are computed properties — NOT mapped to the database.")]
public sealed class CargoDryKitEntity : AizenEntityWithAudit
{
    public string            SerialNumber   { get; private set; } = default!;
    public string            KitCode        { get; private set; } = default!;
    public string            QrPayload      { get; private set; } = default!;
    public string            ProductCode    { get; private set; } = default!;
    public string            BatchCode      { get; private set; } = default!;
    public CargoDryKitStatus Status         { get; private set; }
    public long?             OwnerUserId    { get; private set; }
    public long?             VesselId       { get; private set; }
    public DateTimeOffset    ManufacturedAt { get; private set; }
    public DateTimeOffset?   ActivatedAt    { get; private set; }
    public DateTimeOffset?   ExpiresAt      { get; private set; }
    public int               RenewalCount   { get; private set; }
    public string?           RevokeReason   { get; private set; }
    public DateTimeOffset?   RevokedAt      { get; private set; }

    private CargoDryKitEntity() { }

    public static CargoDryKitEntity Create(
        string serialNumber, string kitCode, string qrPayload,
        string productCode, string batchCode)
        => new()
        {
            SerialNumber   = serialNumber,
            KitCode        = kitCode,
            QrPayload      = qrPayload,
            ProductCode    = productCode,
            BatchCode      = batchCode,
            Status         = CargoDryKitStatus.Available,
            ManufacturedAt = DateTimeOffset.UtcNow,
            IsActive       = true,
        };

    public void Activate(long userId, long vesselId, int validityDays)
    {
        if (Status != CargoDryKitStatus.Available)
            throw new InvalidOperationException(
                $"Kit {SerialNumber} cannot be activated — current status: {Status}");

        Status      = CargoDryKitStatus.Activated;
        OwnerUserId = userId;
        VesselId    = vesselId;
        ActivatedAt = DateTimeOffset.UtcNow;
        ExpiresAt   = DateTimeOffset.UtcNow.AddDays(validityDays);
    }

    public void Renew(int additionalDays, string paymentRef)
    {
        if (Status != CargoDryKitStatus.Activated && Status != CargoDryKitStatus.Expired)
            throw new InvalidOperationException($"Kit {SerialNumber} cannot be renewed — status: {Status}");

        var baseDate  = ExpiresAt.HasValue && ExpiresAt > DateTimeOffset.UtcNow
            ? ExpiresAt.Value
            : DateTimeOffset.UtcNow;
        ExpiresAt     = baseDate.AddDays(additionalDays);
        Status        = CargoDryKitStatus.Activated;
        RenewalCount += 1;
        _ = paymentRef;
    }

    public void MarkExpired()
    {
        if (Status == CargoDryKitStatus.Activated)
            Status = CargoDryKitStatus.Expired;
    }

    public void Revoke(string reason)
    {
        Status       = CargoDryKitStatus.Revoked;
        RevokeReason = reason;
        RevokedAt    = DateTimeOffset.UtcNow;
    }

    public void Transfer(long newUserId, long newVesselId)
    {
        if (Status != CargoDryKitStatus.Activated)
            throw new InvalidOperationException("Only active kits can be transferred.");
        OwnerUserId = newUserId;
        VesselId    = newVesselId;
    }

    public double EfficiencyPercent
    {
        get
        {
            if (!ExpiresAt.HasValue || !ActivatedAt.HasValue) return 0;
            var total     = (ExpiresAt.Value - ActivatedAt.Value).TotalDays;
            var remaining = (ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays;
            return total <= 0 ? 0 : Math.Max(0, Math.Min(100, remaining / total * 100));
        }
    }

    public int DaysUntilExpiry
        => ExpiresAt.HasValue
            ? (int)Math.Max(0, Math.Ceiling((ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays))
            : 0;

    public bool IsExpiringSoon(int withinDays = 30)
        => Status == CargoDryKitStatus.Activated && DaysUntilExpiry <= withinDays;
}
