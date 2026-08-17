namespace Aizen.Modules.Content.Core;

/// <summary>
/// Keycloak/application role names and endpoint role sets for the Content module (§7).
/// Application roles are carried in the Identity token and surfaced via AizenIdentityClaimsTransformation.
/// </summary>
public static class ContentRoles
{
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";
    public const string ContentAdmin = "ContentAdmin";
    public const string ContentEditor = "ContentEditor";
    public const string ContentModerator = "ContentModerator";

    /// <summary>Controller-level authorization for the admin authoring surface (broadest set).</summary>
    public const string AdminAuthoring = "Admin,SuperAdmin,ContentAdmin,ContentEditor";

    /// <summary>Controller-level authorization for comment moderation endpoints (§7).</summary>
    public const string Moderation = "Admin,SuperAdmin,ContentAdmin,ContentModerator";

    /// <summary>
    /// In-handler elevated narrowing: publish / unpublish / archive / delete / category management
    /// are restricted to these roles (ContentEditor may draft/edit/schedule but not these).
    /// </summary>
    public static readonly string[] Elevated = { Admin, SuperAdmin, ContentAdmin };
}
