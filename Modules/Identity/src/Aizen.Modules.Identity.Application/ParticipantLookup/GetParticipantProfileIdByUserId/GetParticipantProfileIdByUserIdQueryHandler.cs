using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;
using Aizen.Modules.Identity.Domain.Interface;

namespace Aizen.Modules.Identity.Application.ParticipantLookup.GetParticipantProfileIdByUserId;

public sealed class GetParticipantProfileIdByUserIdQueryHandler
    : AizenQueryHandler<GetParticipantProfileIdByUserIdQuery, ParticipantProfileIdDto>
{
    private readonly IUserProfileRepository _profiles;

    public GetParticipantProfileIdByUserIdQueryHandler(IUserProfileRepository profiles) => _profiles = profiles;

    public override async Task<ParticipantProfileIdDto?> Handle(
        GetParticipantProfileIdByUserIdQuery request, CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
            return new ParticipantProfileIdDto { UserId = request.UserId, ProfileId = 0 };

        var profile = await _profiles.GetActiveProfileIdAsync(request.UserId, WorkshopRoleContext.Participant);
        return new ParticipantProfileIdDto { UserId = request.UserId, ProfileId = profile?.Id ?? 0 };
    }
}
