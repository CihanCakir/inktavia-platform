using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Onboarding.GetProviderOnboarding;

public sealed class GetProviderOnboardingQuery : AizenQuery<ProviderOnboardingResponse>
{
    public long ProfileId { get; set; }
}
