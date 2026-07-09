using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.SuspendOrganizerProfile;

public sealed class SuspendOrganizerProfileCommand : AizenCommand<SuspendOrganizerProfileResponse>
{
    public long UserId { get; }
    public long ProfileId { get; }
    public string? Reason { get; }

    public SuspendOrganizerProfileCommand(long userId, long profileId, string? reason = null)
    {
        UserId = userId;
        ProfileId = profileId;
        Reason = reason;
    }
}
