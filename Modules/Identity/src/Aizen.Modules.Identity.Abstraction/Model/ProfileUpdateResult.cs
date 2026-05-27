namespace Aizen.Modules.Identity.Abstraction.Model
{
    public sealed record ProfileUpdateResult(
        bool Success,
        WorkshopRoleContext RoleContext,
        long UserId,
        long ProfileId,
        string? Message
    );
}