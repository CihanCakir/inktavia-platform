using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Identity.Application.AdminUsers.GetAdminUserIds;

/// <summary>N-D — the numeric UserIds of admin users (to fan out a support-request notification).</summary>
public sealed class GetAdminUserIdsQuery : AizenListedQuery<long>
{
}
