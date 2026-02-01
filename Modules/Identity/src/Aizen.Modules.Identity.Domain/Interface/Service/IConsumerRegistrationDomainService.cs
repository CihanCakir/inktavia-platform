using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Entities;

namespace Aizen.Modules.Identity.Domain.Interface.Service
{
    public interface IConsumerRegistrationDomainService
    {
        Task<(UserEntity user, UserProfileEntity profile)> RegisterOrAttachAsync(
        RegisterParticipantDomainModel m, CancellationToken ct);
    }
}