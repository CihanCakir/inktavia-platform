using Aizen.Modules.Identity.Domain.Entities.Onboarding;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Identity.Repository.Identity.Repository;

public sealed class ProviderOnboardingRepository : IProviderOnboardingRepository
{
    private readonly IdentityDbContext _db;

    public ProviderOnboardingRepository(IdentityDbContext db) => _db = db;

    public Task<ProviderOnboardingEntity?> GetByProfileIdAsync(long profileId, CancellationToken ct = default)
        => _db.ProviderOnboarding.FirstOrDefaultAsync(x => x.ProfileId == profileId && !x.IsDeleted, ct);

    public Task<ProviderOnboardingEntity?> GetByUserIdAsync(long userId, CancellationToken ct = default)
        => _db.ProviderOnboarding.FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted, ct);

    public async Task AddAsync(ProviderOnboardingEntity entity, CancellationToken ct = default)
    {
        await _db.ProviderOnboarding.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public void Update(ProviderOnboardingEntity entity)
    {
        _db.ProviderOnboarding.Update(entity);
    }
}
