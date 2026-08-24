using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Repository;

public interface INotificationTemplateContentRepository
{
    /// <summary>
    /// Bir template kodu + kanal için TÜM Published içerikleri (tüm locale/version) döner. Locale fallback ve
    /// version seçimi renderer'da yapılır. Join: notification_templates.TemplateCode → TemplateId.
    /// </summary>
    Task<List<NotificationTemplateContentEntity>> GetPublishedByTemplateCodeAndChannelAsync(
        string templateCode, NotificationChannel channel, CancellationToken ct = default);

    /// <summary>Bir mantıksal template'in TÜM içerik satırları (her kanal/locale/version/status). Matris/liste için.</summary>
    Task<List<NotificationTemplateContentEntity>> GetAllByTemplateIdAsync(long templateId, CancellationToken ct = default);

    /// <summary>(templateId, channel, locale) için mevcut Draft satırı (varsa; en çok bir tane olmalı).</summary>
    Task<NotificationTemplateContentEntity?> GetCurrentDraftAsync(
        long templateId, NotificationChannel channel, string locale, CancellationToken ct = default);

    /// <summary>(templateId, channel, locale) için mevcut Published satırı (varsa).</summary>
    Task<NotificationTemplateContentEntity?> GetCurrentPublishedAsync(
        long templateId, NotificationChannel channel, string locale, CancellationToken ct = default);

    /// <summary>(templateId, channel, locale) için tüm sürümler, en yeni önce (versiyon listesi).</summary>
    Task<List<NotificationTemplateContentEntity>> GetVersionsAsync(
        long templateId, NotificationChannel channel, string locale, CancellationToken ct = default);

    /// <summary>(templateId, channel, locale) için en yüksek Version (yoksa 0). Yeni draft versiyonu için.</summary>
    Task<int> GetMaxVersionAsync(
        long templateId, NotificationChannel channel, string locale, CancellationToken ct = default);

    Task AddAsync(NotificationTemplateContentEntity entity, CancellationToken ct = default);
    Task UpdateAsync(NotificationTemplateContentEntity entity, CancellationToken ct = default);
}
