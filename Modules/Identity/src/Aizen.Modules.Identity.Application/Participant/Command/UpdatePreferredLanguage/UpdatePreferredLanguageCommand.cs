using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Model;

namespace Aizen.Modules.InktaviaStore.Application.Identity
{
    /// <summary>
    /// Oturum açmış kullanıcının kalıcı dil tercihini günceller. Tercih UserEntity üzerindedir (profilde değil).
    /// </summary>
    public sealed class UpdatePreferredLanguageCommand : AizenCommand<UpdatePreferredLanguageResult>
    {
        public string? PreferredLanguage { get; }

        public UpdatePreferredLanguageCommand(string? preferredLanguage) => PreferredLanguage = preferredLanguage;
    }
}
