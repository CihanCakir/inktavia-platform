
namespace Aizen.Modules.Identity.Abstraction.Response
{
    public sealed record CompleteExternalLoginParticipantResponse(
    long UserId,
    long ActiveProfileId,
    string AccessToken,
    string RefreshToken,
    bool IsNewUser,
    bool NeedsOnboarding,
    bool EmailVerified
);
}