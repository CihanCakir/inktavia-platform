namespace Aizen.Modules.Identity.Abstraction.Dto.Common;

/// <summary>
/// Bir profilin bağlı kullanıcısının kalıcı dil tercihi (<c>UserProfiles.Id</c> ile anahtarlanır — hem participant
/// hem provider/organizer profili için çalışır; ikisi de UserProfiles satırıdır). Notification modülü, alıcıya doğru
/// dilde bildirim göndermek için bu iç okuma modelini kullanır. <see cref="PreferredLanguage"/> tercih yoksa null'dır.
/// </summary>
public sealed class ProfilePreferredLanguageDto
{
    public long ProfileId { get; set; }
    public string? PreferredLanguage { get; set; }
}
