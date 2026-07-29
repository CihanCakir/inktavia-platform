using Aizen.Modules.Payment.Domain.Entities.Economics;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

/// <summary>
/// Insert + read persistence for <see cref="PaymentEconomicsSnapshotEntity"/>.
/// No Update/Remove — the snapshot is immutable (§5).
/// </summary>
public sealed class PaymentEconomicsSnapshotRepository : IPaymentEconomicsSnapshotRepository
{
    private readonly PaymentDbContext _db;
    public PaymentEconomicsSnapshotRepository(PaymentDbContext db) => _db = db;

    public Task AddAsync(PaymentEconomicsSnapshotEntity entity, CancellationToken ct = default)
        => _db.PaymentEconomicsSnapshots.AddAsync(entity, ct).AsTask();

    public Task<PaymentEconomicsSnapshotEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.PaymentEconomicsSnapshots.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<PaymentEconomicsSnapshotEntity?> GetByCodeAsync(string snapshotCode, CancellationToken ct = default)
        => _db.PaymentEconomicsSnapshots.FirstOrDefaultAsync(x => x.SnapshotCode == snapshotCode, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
