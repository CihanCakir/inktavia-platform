using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.InktaviaStore.Application.Identity
{
    public class RejectVenueProfileCommand : AizenCommand<VenueOrganizationRegistrationResponse>
    {
        public long UserId { get; }
        public long ProfileId { get; }
        public string? Reason { get; set; }
        public string? ReasonCategory { get; set; }
        public string? InternalNote { get; set; }
        public bool NotifyUser { get; set; } = true;
        public string? ReviewedBy { get; set; }

        public RejectVenueProfileCommand(long userId, long profileId, string? reason = null,
            string? reasonCategory = null, string? internalNote = null, bool notifyUser = true,
            string? reviewedBy = null)
        {
            UserId = userId;
            ProfileId = profileId;
            Reason = reason;
            ReasonCategory = reasonCategory;
            InternalNote = internalNote;
            NotifyUser = notifyUser;
            ReviewedBy = reviewedBy;
        }
    }
}