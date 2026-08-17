using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Entities;

namespace Aizen.Modules.Identity.Domain.Interface.Service
{
    /// <summary>
    /// Idempotently provisions/links an Organizer profile for a Keycloak-authenticated user.
    /// No local password is created; the legacy IOAuthProviderClient is never used.
    /// </summary>
    public interface IOrganizerKeycloakProvisioningDomainService
    {
        Task<OrganizerKeycloakProvisionResult> ProvisionAsync(
            ProvisionOrganizerFromKeycloakDomainModel model, CancellationToken cancellationToken = default);
    }

    public sealed record OrganizerKeycloakProvisionResult(
        UserEntity User,
        UserProfileEntity Profile,
        bool CreatedUser,
        bool CreatedProfile,
        bool AlreadyLinked,
        List<string> Warnings);
}
