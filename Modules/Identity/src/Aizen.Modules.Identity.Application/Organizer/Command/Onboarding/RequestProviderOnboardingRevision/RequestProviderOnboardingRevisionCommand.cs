using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.RequestProviderOnboardingRevision;

public sealed class RequestProviderOnboardingRevisionCommand : AizenCommand<RequestProviderOnboardingRevisionResponse>
{
    public long ProfileId { get; set; }
    public string[] Steps { get; set; } = Array.Empty<string>();
    public string Note { get; set; } = default!;
}
