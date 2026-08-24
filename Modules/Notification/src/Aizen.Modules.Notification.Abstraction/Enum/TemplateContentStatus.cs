namespace Aizen.Modules.Notification.Abstraction.Enum;

/// <summary>
/// Şablon İÇERİĞİNİN yayın durumu (NotificationStatus'tan ayrı — o teslimat durumudur). Yalnızca bir
/// (channel, locale, version) içeriğinin taslak/yayında/arşiv yaşam döngüsünü tutar.
/// </summary>
public enum TemplateContentStatus
{
    Draft     = 0,
    Published = 1,
    Archived  = 2,
}
