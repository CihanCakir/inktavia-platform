using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Entities;

namespace Aizen.Modules.Identity.Domain.Interface.Service
{
    public interface IVenueRegistrationDomainService
    {
        Task<(UserEntity user, UserProfileEntity profile)> RegisterOrAttachAsync(RegisterVenueDomainModel m, CancellationToken ct);
    }
}