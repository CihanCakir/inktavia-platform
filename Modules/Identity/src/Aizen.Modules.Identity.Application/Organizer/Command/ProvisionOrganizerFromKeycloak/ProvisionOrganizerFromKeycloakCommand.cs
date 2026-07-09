using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.ProvisionOrganizerFromKeycloak;

public sealed class ProvisionOrganizerFromKeycloakCommand : AizenCommand<ProvisionOrganizerFromKeycloakResult>
{
    public string KeycloakSubjectId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? ContactPhone { get; set; }
    public string? TaxNo { get; set; }
    public bool? EmailVerified { get; set; }
}
