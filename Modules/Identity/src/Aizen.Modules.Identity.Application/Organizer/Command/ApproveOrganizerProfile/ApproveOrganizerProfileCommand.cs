using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.InktaviaStore.Application.Identity
{
    public class ApproveOrganizerProfileCommand : AizenCommand<VenueOrganizationRegistrationResponse>
    {
        public long UserId { get; }
        public long ProfileId { get; }
        public string? ReviewedBy { get; }

        public ApproveOrganizerProfileCommand(long userId, long profileId, string? reviewedBy = null)
        {
            UserId = userId;
            ProfileId = profileId;
            ReviewedBy = reviewedBy;
        }
    }
}