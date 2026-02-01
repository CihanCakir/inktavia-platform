using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command
{
    public class StartExternalLoginParticipantCommand : AizenCommand<StartExternalLoginResponse>
    {
        public string Provider { get; } // "google" | "apple"
        public string? RedirectAfterLogin { get; }  // optional
        public string? UiLocale { get; } // "tr"|"en" -> (apple/google &prompt'lara eklenebilir)

        public StartExternalLoginParticipantCommand(
            string provider,
            string? redirectAfterLogin,
            string? uiLocale
        )
        {
            Provider = provider;
            RedirectAfterLogin = redirectAfterLogin;
            UiLocale = uiLocale;
        }
    }
}