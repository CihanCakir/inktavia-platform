namespace Aizen.Modules.Identity.Abstraction.Response
{
    public sealed record SuspendOrganizerProfileResponse(
        bool Success,
        long UserId,
        long ProfileId,
        string ProfileStatus,
        string ApprovalStatus,
        string Message);
}
