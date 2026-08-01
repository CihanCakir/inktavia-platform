using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Entities;

namespace Aizen.Modules.Identity.Domain.Interface.Service;

public interface IAdminKeycloakProvisioningDomainService
{
    Task<AdminKeycloakProvisionResult> ProvisionAsync(
        ProvisionAdminFromKeycloakDomainModel model, CancellationToken cancellationToken = default);
}

public sealed record AdminKeycloakProvisionResult(
    UserEntity User,
    UserProfileEntity? Profile,
    bool CreatedUser,
    bool CreatedProfile,
    bool AlreadyLinked,
    List<string> Warnings);
