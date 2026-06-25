using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Abstraction.Response
{
    public sealed record VenueOrganizationRegistrationResponse(
    bool Success,
    string Action,                // "Approve" | "Reject"
    WorkshopRoleContext Context,
    long UserId,
    long ProfileId,
    ApprovalStatus NewStatus,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    string? RejectReason,
    string? Message,
    string? ReviewedBy = null,
    string? ReviewedAt = null,
    string? RejectionCategory = null
);
}