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

    public Task AddAsync(ProviderPaymentProfileEntity entity, CancellationToken ct)
        => _db.PaymentProfiles.AddAsync(entity, ct).AsTask();

    public void Update(ProviderPaymentProfileEntity entity) => _db.PaymentProfiles.Update(entity);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
