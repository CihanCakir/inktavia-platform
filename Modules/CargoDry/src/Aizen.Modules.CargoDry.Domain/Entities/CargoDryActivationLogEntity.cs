using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

public sealed class CargoDryActivationLogEntity : AizenEntity
{
    public long             KitId            { get; private set; }
    public long             UserId           { get; private set; }
    public long             VesselId         { get; private set; }
    public DateTimeOffset   ActivatedAt      { get; private set; }
    public ActivationMethod Method           { get; private set; }
    public ActivationSource Source           { get; private set; }
    public string?          DeviceInfo       { get; private set; }
    public string?          IpAddress        { get; private set; }

    private CargoDryActivationLogEntity() { }

    public static CargoDryActivationLogEntity Create(
        long kitId, long userId, long vesselId,
        ActivationMethod method, ActivationSource source,
        string? deviceInfo = null, string? ipAddress = null)
        => new()
        {
            KitId       = kitId,
            UserId      = userId,
            VesselId    = vesselId,
            ActivatedAt = DateTimeOffset.UtcNow,
            Method      = method,
            Source      = source,
            DeviceInfo  = deviceInfo,
            IpAddress   = ipAddress,
        };
}
