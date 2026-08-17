using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.SubmitProviderOnboarding;

public sealed class SubmitProviderOnboardingCommand : AizenCommand<SubmitProviderOnboardingResponse>
{
    public long ProfileId { get; set; }
}
