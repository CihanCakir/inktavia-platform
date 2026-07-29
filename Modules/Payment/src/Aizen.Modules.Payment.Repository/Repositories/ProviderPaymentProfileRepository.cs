using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

public sealed class ProviderPaymentProfileRepository : IProviderPaymentProfileRepository
{
    private readonly PaymentDbContext _db;
    public ProviderPaymentProfileRepository(PaymentDbContext db) => _db = db;

    public Task<ProviderPaymentProfileEntity?> GetByProviderProfileIdAsync(long providerProfileId, CancellationToken ct)
        => _db.PaymentProfiles.FirstOrDefaultAsync(x => x.ProviderProfileId == providerProfileId, ct);

    public Task<bool> ExistsAsync(long providerProfileId, CancellationToken ct)
        => _db.PaymentProfiles.AnyAsync(x => x.ProviderProfileId == providerProfileId, ct);

    public async Task<ProviderSplitEligibility> GetSplitEligibilityAsync(long providerProfileId, CancellationToken ct)
    {
        var profile = await _db.PaymentProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProviderProfileId == providerProfileId, ct);

        return profile is null
            ? new ProviderSplitEligibility(HasProfile: false, IsSplitEligible: false,
                OnboardingStatus: Abstraction.Enum.ProviderSubMerchantOnboardingStatus.NotStarted)
            : new ProviderSplitEligibility(HasProfile: true, profile.IsSplitEligible, profile.OnboardingStatus,
                profile.SubMerchantKey);
    }

    public async Task<(List<ProviderPaymentProfileEntity> Items, int Total)> GetOnboardingQueueAsync(
        Abstraction.Enum.ProviderSubMerchantOnboardingStatus? status, int skip, int take, CancellationToken ct = default)
    {
        var q = _db.PaymentProfiles.AsNoTracking();
        q = status.HasValue
            ? q.Where(x => x.OnboardingStatus == status.Value)
            // Default queue = items needing attention (exclude the terminal-happy NotStarted / Verified extremes).
            : q.Where(x => x.OnboardingStatus != Abstraction.Enum.ProviderSubMerchantOnboardingStatus.NotStarted
                        && x.OnboardingStatus != Abstraction.Enum.ProviderSubMerchantOnboardingStatus.Verified);

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(x => x.OnboardingStatus).ThenBy(x => x.ProviderProfileId)
            .Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public Task AddAsync(ProviderPaymentProfileEntity entity, CancellationToken ct)
        => _db.PaymentProfiles.AddAsync(entity, ct).AsTask();

    public void Update(ProviderPaymentProfileEntity entity) => _db.PaymentProfiles.Update(entity);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
