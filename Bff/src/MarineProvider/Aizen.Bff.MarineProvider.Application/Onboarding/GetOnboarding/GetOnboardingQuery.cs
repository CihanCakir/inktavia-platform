using Aizen.Bff.MarineProvider.Application.Contracts.Onboarding;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Onboarding.GetOnboarding;

public sealed class GetOnboardingQuery : AizenQuery<OnboardingResponse>
{
}
