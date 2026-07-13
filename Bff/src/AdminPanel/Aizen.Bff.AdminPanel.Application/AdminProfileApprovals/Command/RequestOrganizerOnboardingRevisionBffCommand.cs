using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Command;

[DocumentationInfo("Request organizer onboarding revision BFF command",
    "Admin sends specific onboarding steps back for provider revision, with a required note explaining what needs to be fixed.")]
public sealed class RequestOrganizerOnboardingRevisionBffCommand
    : AizenCommand<OnboardingRevisionBffResponse>
{
    public long UserId { get; }
    public long ProfileId { get; }

    /// <summary>camelCase step names, e.g. ["businessIdentity", "complianceVerification"]</summary>
    public string[] Steps { get; }

    /// <summary>Admin note explaining what needs to be revised. 10–2000 chars.</summary>
    public string Note { get; }

    public RequestOrganizerOnboardingRevisionBffCommand(
        long userId, long profileId, string[] steps, string note)
    {
        UserId    = userId;
        ProfileId = profileId;
        Steps     = steps;
        Note      = note;
    }
}
