using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Dto;

[DocumentationInfo("Admin user overview response", "Aggregated paged lists of organizer, venue and participant profiles for the admin identity overview.")]
public sealed class AdminUserOverviewResponse
{
    public PagedOrganizerProfileResult? Organizers { get; set; }
    public PagedVenueProfileResult? Venues { get; set; }
    public PagedParticipantProfileResult? Participants { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
