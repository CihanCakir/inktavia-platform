using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.InktaviaStore.Application.Identity
{
    public class RejectOrganizerProfileCommand : AizenCommand<VenueOrganizationRegistrationResponse>
    {
        public long UserId { get; }
        public long ProfileId { get; }
        public string? Reason { get; set; }

        public RejectOrganizerProfileCommand(long userId, long profileId, string? reason = null)
        {
            UserId = userId;
            ProfileId = profileId;
            Reason = reason;    
        }
    }
}