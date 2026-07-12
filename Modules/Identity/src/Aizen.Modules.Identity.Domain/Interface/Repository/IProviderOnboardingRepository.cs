using Aizen.Modules.Identity.Domain.Entities.Onboarding;

namespace Aizen.Modules.Identity.Domain.Interface.Repository;

public interface IProviderOnboardingRepository
{
    Task<ProviderOnboardingEntity?> GetByProfileIdAsync(long profileId, CancellationToken ct = default);
    Task<ProviderOnboardingEntity?> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task AddAsync(ProviderOnboardingEntity entity, CancellationToken ct = default);
    void Update(ProviderOnboardingEntity entity);
}
