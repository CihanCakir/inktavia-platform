using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Participant.ProvisionParticipantFromKeycloak;

public sealed class ProvisionParticipantFromKeycloakCommandHandler
    : AizenCommandHandler<ProvisionParticipantFromKeycloakCommand, ProvisionParticipantFromKeycloakResult>
{
    private readonly IParticipantKeycloakProvisioningDomainService _service;

    public ProvisionParticipantFromKeycloakCommandHandler(IParticipantKeycloakProvisioningDomainService service)
    {
        _service = service;
    }

    public override async Task<ProvisionParticipantFromKeycloakResult?> Handle(
        ProvisionParticipantFromKeycloakCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.ProvisionAsync(new ProvisionParticipantFromKeycloakDomainModel
        {
            KeycloakSubjectId = request.KeycloakSubjectId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            ContactPhone = request.ContactPhone,
            EmailVerified = request.EmailVerified
        }, cancellationToken);

        return new ProvisionParticipantFromKeycloakResult
        {
            ParticipantProfileId = result.Profile.Id,
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
