using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.MarkOrganizerPhoneVerified;

public sealed class MarkOrganizerPhoneVerifiedCommand : AizenCommand<MarkOrganizerPhoneVerifiedResult>
{
    public long ProfileId { get; }

    public MarkOrganizerPhoneVerifiedCommand(long profileId)
    {
        ProfileId = profileId;
    }
}

public sealed record MarkOrganizerPhoneVerifiedResult(long ProfileId, bool PhoneVerified);
