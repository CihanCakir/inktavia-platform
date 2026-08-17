using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Profile;

/// <summary>PUT /api/v1/mobile/profile/me — update the authenticated participant's editable profile fields.</summary>
public sealed class UpdateParticipantProfileBffCommand : AizenCommand<GetParticipantProfileResponse>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? NationalityId { get; set; }
}
