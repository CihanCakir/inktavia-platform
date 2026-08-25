using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Identity.Application.AdminUsers.GetAllParticipantProfileIds;

/// <summary>
/// Faz 28.6 — tüm aktif katılımcı profil id'leri (ids only, no PII). Notification modülünün admin kampanya "All"
/// genişletmesi için. GetAdminUserIds ile aynı iç-okuma deseni.
/// </summary>
public sealed class GetAllParticipantProfileIdsQuery : AizenListedQuery<long>
{
}
