using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.ReactivateOrganizerProfile;

public sealed class ReactivateOrganizerProfileCommand : AizenCommand<ReactivateOrganizerProfileResponse>
{
    public long UserId { get; }
    public long ProfileId { get; }

    public ReactivateOrganizerProfileCommand(long userId, long profileId)
    {
        UserId = userId;
        ProfileId = profileId;
    }
}
