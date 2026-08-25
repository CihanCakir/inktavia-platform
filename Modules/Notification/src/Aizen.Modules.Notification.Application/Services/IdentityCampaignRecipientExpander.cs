using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// "All" genişletmeyi Identity'nin iç ids-only uçlarından (providers/all-ids, participants/all-ids) yapar — bu uçlar
/// mevcut profil sorgularını yeniden kullanır. Dönen değerler PROFİL id'leridir (NotificationEntity.RecipientUserId
/// için beklenen anahtar; locale/e-posta çözümleri de profil id ile çalışır).
/// </summary>
public sealed class IdentityCampaignRecipientExpander : ICampaignRecipientExpander
{
    private readonly INotificationIdentityRemoteCall _identity;

    public IdentityCampaignRecipientExpander(INotificationIdentityRemoteCall identity) => _identity = identity;

    public async Task<IReadOnlyList<long>> ExpandAsync(CampaignAudience audience, CancellationToken ct = default)
    {
        var response = audience switch
        {
            CampaignAudience.Provider    => await _identity.GetAllProviderProfileIds(),
            CampaignAudience.Participant => await _identity.GetAllParticipantProfileIds(),
            _ => null,
        };

        return response?.Body?.Where(id => id > 0).Distinct().ToList() ?? new List<long>();
    }
}
