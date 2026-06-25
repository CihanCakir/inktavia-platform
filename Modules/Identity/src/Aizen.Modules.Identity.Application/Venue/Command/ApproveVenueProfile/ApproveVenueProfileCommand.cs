using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.InktaviaStore.Application.Identity
{
    public sealed class ApproveVenueProfileCommand : AizenCommand<VenueOrganizationRegistrationResponse>
    {
        public long UserId { get; }
        public long ProfileId { get; }
        public string? ReviewedBy { get; }

        public ApproveVenueProfileCommand(long userId, long profileId, string? reviewedBy = null)
        {
            UserId = userId;
            ProfileId = profileId;
            ReviewedBy = reviewedBy;
        }
    }
}