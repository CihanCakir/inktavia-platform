using Aizen.Modules.Payment.Domain.Entities.Economics;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

/// <summary>
/// Persistence for the immutable <see cref="PaymentEconomicsSnapshotEntity"/>.
/// Insert + read only — there is intentionally <b>no Update/Remove</b>: the snapshot is immutable (§5).
/// </summary>
public interface IPaymentEconomicsSnapshotRepository
{
    Task AddAsync(PaymentEconomicsSnapshotEntity entity, CancellationToken ct = default);

    Task<PaymentEconomicsSnapshotEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<PaymentEconomicsSnapshotEntity?> GetByCodeAsync(string snapshotCode, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
