namespace Aizen.Modules.Notification.Abstraction.Enum;

/// <summary>Kampanya yaşam döngüsü. Terminal: Completed (kısmi başarı dahil) veya Failed (her satır başarısızsa).</summary>
public enum CampaignStatus
{
    Created    = 0,
    Queued     = 1,
    Processing = 2,
    Completed  = 3,
    Failed     = 4,
}
