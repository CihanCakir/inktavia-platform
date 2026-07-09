namespace Aizen.Modules.Identity.Abstraction.Response
{
    public sealed record ReactivateOrganizerProfileResponse(
        bool Success,
        long UserId,
        long ProfileId,
        string ProfileStatus,
        string ApprovalStatus,
        string Message);
}
