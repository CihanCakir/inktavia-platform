namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Kampanyaların desteklediği diller (v1: tr + en). Custom içerik bu dillerin HEPSİNİ kapsamalı (kısmi reddedilir),
/// şablon yolunda ise bu dillerden EN AZ BİRİ için yayınlanmış içerik bulunmalı. Tek doğru kaynak.
/// </summary>
public static class CampaignLocales
{
    public static readonly IReadOnlyList<string> Supported = new[] { "tr", "en" };
}
