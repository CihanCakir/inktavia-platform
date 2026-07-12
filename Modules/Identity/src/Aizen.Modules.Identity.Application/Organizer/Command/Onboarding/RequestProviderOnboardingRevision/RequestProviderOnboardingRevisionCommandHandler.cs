using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.RequestProviderOnboardingRevision;

public sealed class RequestProviderOnboardingRevisionCommandHandler
    : AizenCommandHandler<RequestProviderOnboardingRevisionCommand, RequestProviderOnboardingRevisionResponse>
{
    private readonly IProviderOnboardingDomainService _service;
    public RequestProviderOnboardingRevisionCommandHandler(IProviderOnboardingDomainService service) => _service = service;

    public override async Task<RequestProviderOnboardingRevisionResponse?> Handle(
        RequestProviderOnboardingRevisionCommand request, CancellationToken ct)
    {
        await _service.RequestRevisionAsync(request.ProfileId, request.Steps, request.Note, ct);
        return new RequestProviderOnboardingRevisionResponse { Success = true, Message = "Revision requested." };
    }
}
