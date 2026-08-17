using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Domain.Entities
{
public class UserDeviceBlockEntity : AizenEntityWithAudit
{
    public string DeviceName { get; private set; } = default!;
    public string DeviceId { get; private set; } = default!;
    public int? DeviceTypeId { get; private set; }
    public string IpAddress { get; private set; } = default!;
    public bool IsActive { get; private set; }

    // EF Core lazy-loading proxies (Castle DynamicProxy) subclass the entity, so the parameterless ctor must be
    // at least protected. A private one makes every query that materializes this type fail at runtime.
    protected UserDeviceBlockEntity() { }

    /// <summary>
    /// Yeni bir cihaz blok kaydı oluşturur (factory)
    /// </summary>
    public static UserDeviceBlockEntity Create(
        string deviceName,
        string deviceId,
        string ipAddress,
        int? deviceTypeId = null)
    {
        return new UserDeviceBlockEntity
        {
            DeviceName = deviceName,
            DeviceId = deviceId,
            IpAddress = ipAddress,
            DeviceTypeId = deviceTypeId,
            CreateDate = DateTime.UtcNow,
            IsActive = true
        };
    }

    /// <summary>
    /// Cihaz blok kaydını pasif hale getirir
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        ModifyDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Cihaz blok kaydını aktif hale getirir (tekrar engelle)
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        ModifyDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Durumu tersine çevirir (toggle)
    /// </summary>
    public void ToggleActivity()
    {
        IsActive = !IsActive;
        ModifyDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Bloklama tarihi aynı gün mü kontrolü (iş kuralı)
    /// </summary>
    public bool IsBlockedToday(DateTime today)
    {
        return IsActive && CreateDate.HasValue && CreateDate.Value.Date == today.Date;
    }
}

}