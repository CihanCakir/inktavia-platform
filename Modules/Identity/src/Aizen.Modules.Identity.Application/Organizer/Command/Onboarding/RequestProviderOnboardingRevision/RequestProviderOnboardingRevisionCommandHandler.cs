using Aizen.Core.CQRS.Handler;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Abstraction.Message;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.RequestProviderOnboardingRevision;

public sealed class RequestProviderOnboardingRevisionCommandHandler
    : AizenCommandHandler<RequestProviderOnboardingRevisionCommand, RequestProviderOnboardingRevisionResponse>
{
    private readonly IProviderOnboardingDomainService _service;
    private readonly IUserProfileRepository _profileRepo;
    private readonly UserManager<UserEntity> _userManager;
    private readonly IAizenMessagePublisher _publisher;

    public RequestProviderOnboardingRevisionCommandHandler(
        IProviderOnboardingDomainService service,
        IUserProfileRepository profileRepo,
        UserManager<UserEntity> userManager,
        IAizenMessagePublisher publisher)
    {
        _service = service;
        _profileRepo = profileRepo;
        _userManager = userManager;
        _publisher = publisher;
    }

    public override async Task<RequestProviderOnboardingRevisionResponse?> Handle(
        RequestProviderOnboardingRevisionCommand request, CancellationToken ct)
    {
        await _service.RequestRevisionAsync(request.ProfileId, request.Steps, request.Note, ct);

        var profile = await _profileRepo.GetProfileByIdAsync(request.ProfileId);
        if (profile is not null)
        {
            var user = await _userManager.FindByIdAsync(profile.UserId.ToString());
            await _publisher.PublishAsync(new ProviderOnboardingRevisionRequestedMessage
            {
                ProfileId = request.ProfileId,
                UserId = profile.UserId,
                Email = user?.Email,
                Steps = request.Steps,
                Note = request.Note,
                RequestedAtUtc = DateTime.UtcNow,
            }, ct);
        }

        return new RequestProviderOnboardingRevisionResponse { Success = true, Message = "Revision requested." };
    }
}
