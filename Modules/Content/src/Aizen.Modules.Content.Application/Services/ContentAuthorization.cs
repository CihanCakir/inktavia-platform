using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Core;

namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// In-handler role narrowing for elevated authoring actions (§7). The controller attribute already
/// gates the whole surface to Admin/SuperAdmin/ContentAdmin/ContentEditor; this narrows the elevated
/// actions (publish/unpublish/archive/delete/category management) to Admin/SuperAdmin/ContentAdmin only.
///
/// Roles are read from IAizenInfoAccessor.UserInfoAccessor.UserInfo.Roles — the application roles
/// projected from the Identity token by AizenIdentityClaimsTransformation (the same pipeline that
/// backs [Authorize(Roles = ...)]).
/// </summary>
public static class ContentAuthorization
{
    public static bool IsElevated(IAizenInfoAccessor info)
    {
        var roles = info.UserInfoAccessor?.UserInfo?.Roles;
        return roles is not null && roles.Any(r => ContentRoles.Elevated.Contains(r));
    }

    public static void EnsureElevated(IAizenInfoAccessor info, string action)
    {
        if (!IsElevated(info))
            throw new AizenBusinessException(
                $"'{action}' requires elevated content privileges (Admin, SuperAdmin or ContentAdmin).");
    }
}
