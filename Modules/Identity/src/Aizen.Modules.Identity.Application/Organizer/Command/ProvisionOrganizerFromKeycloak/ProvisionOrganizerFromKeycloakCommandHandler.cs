using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.ProvisionOrganizerFromKeycloak;

public sealed class ProvisionOrganizerFromKeycloakCommandHandler
    : AizenCommandHandler<ProvisionOrganizerFromKeycloakCommand, ProvisionOrganizerFromKeycloakResult>
{
    private readonly IOrganizerKeycloakProvisioningDomainService _service;

    public ProvisionOrganizerFromKeycloakCommandHandler(IOrganizerKeycloakProvisioningDomainService service)
    {
        _service = service;
    }

    public override async Task<ProvisionOrganizerFromKeycloakResult?> Handle(
        ProvisionOrganizerFromKeycloakCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.ProvisionAsync(new ProvisionOrganizerFromKeycloakDomainModel
        {
            KeycloakSubjectId = request.KeycloakSubjectId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CompanyName = request.CompanyName,
            ContactPhone = request.ContactPhone,
            TaxNo = request.TaxNo,
            EmailVerified = request.EmailVerified
        }, cancellationToken);

        return new ProvisionOrganizerFromKeycloakResult
        {
            ProviderProfileId = result.Profile.Id,
            UserId = result.User.Id,
            CreatedUser = result.CreatedUser,
            CreatedProfile = result.CreatedProfile,
            AlreadyLinked = result.AlreadyLinked,
            ApprovalStatus = result.Profile.ApprovalStatus.ToString(),
            ProfileStatus = result.Profile.Status.ToString(),
            Warnings = result.Warnings
        };
    }
}
