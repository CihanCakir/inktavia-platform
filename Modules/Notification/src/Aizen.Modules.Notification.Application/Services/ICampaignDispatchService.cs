namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Bir kampanyayı yükleyip alıcıları genişletir, locale'e göre gruplar, mevcut kanal dispatcher'ları üzerinden
/// dağıtır ve sayaçları/terminal durumu günceller. Consumer bunu çağırır; testler doğrudan bunu hedefler.
/// </summary>
public interface ICampaignDispatchService
{
    Task DispatchAsync(long campaignId, CancellationToken ct = default);
}
