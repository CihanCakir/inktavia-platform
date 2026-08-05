using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Entities;

namespace Aizen.Modules.Identity.Domain.Interface.Service
{
    /// <summary>
    /// Idempotently provisions/links a Participant (mobile) profile for a Keycloak-authenticated user.
    /// Mirrors <see cref="IOrganizerKeycloakProvisioningDomainService"/>; no local password is created.
    /// </summary>
    public interface IParticipantKeycloakProvisioningDomainService
    {
        Task<ParticipantKeycloakProvisionResult> ProvisionAsync(
            ProvisionParticipantFromKeycloakDomainModel model, CancellationToken cancellationToken = default);
    }

    public sealed record ParticipantKeycloakProvisionResult(
        UserEntity User,
        UserProfileEntity Profile,
        bool CreatedUser,
        bool CreatedProfile,
        bool AlreadyLinked,
        List<string> Warnings);
}
