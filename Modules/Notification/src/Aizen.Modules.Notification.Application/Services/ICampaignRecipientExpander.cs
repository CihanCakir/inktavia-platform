using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// TargetMode=All için hedef kitleyi profil id listesine genişletir. Mevcut Identity admin liste sorgularını (remote
/// call) yeniden kullanır — başka bir modülün tablolarına doğrudan SQL YAZMAZ.
/// </summary>
public interface ICampaignRecipientExpander
{
    Task<IReadOnlyList<long>> ExpandAsync(CampaignAudience audience, CancellationToken ct = default);
}
