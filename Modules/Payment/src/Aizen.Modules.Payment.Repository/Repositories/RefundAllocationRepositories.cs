using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Repository.Repositories;

/// <summary>BE-P10 — versioned refund-allocation policy persistence + pure resolution.</summary>
public sealed class RefundAllocationPolicyRepository : IRefundAllocationPolicyRepository
{
    private readonly PaymentDbContext _db;
    public RefundAllocationPolicyRepository(PaymentDbContext db) => _db = db;

    public async Task<RefundAllocationPolicyEntity?> ResolveAsync(string currency, DateTime atUtc, CancellationToken ct = default)
    {
        var cur = currency.ToUpperInvariant();
        var active = await _db.RefundAllocationPolicies
            .Include(x => x.Rules)
            .AsNoTracking()
            .Where(x => x.CurrencyCode == cur && x.IsActive)
            .ToListAsync(ct);
        return RefundAllocationPolicyResolver.Resolve(active, cur, atUtc);
    }

    public Task<RefundAllocationPolicyEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.RefundAllocationPolicies.Include(x => x.Rules).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<List<RefundAllocationPolicyEntity>> GetAllAsync(CancellationToken ct = default)
        => await _db.RefundAllocationPolicies.Include(x => x.Rules).OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);

    public Task AddAsync(RefundAllocationPolicyEntity entity, CancellationToken ct = default)
        => _db.RefundAllocationPolicies.AddAsync(entity, ct).AsTask();

    public void Update(RefundAllocationPolicyEntity entity) => _db.RefundAllocationPolicies.Update(entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>BE-P10 §7.4 — provider negative-balance ledger persistence.</summary>
public sealed class ProviderBalanceRepository : IProviderBalanceRepository
{
    private readonly PaymentDbContext _db;
    public ProviderBalanceRepository(PaymentDbContext db) => _db = db;

    public Task<ProviderBalanceEntity?> GetByProviderAsync(long providerProfileId, string currency, CancellationToken ct = default)
    {
        var cur = currency.ToUpperInvariant();
        return _db.ProviderBalances.Include(x => x.Movements)
            .FirstOrDefaultAsync(x => x.ProviderProfileId == providerProfileId && x.CurrencyCode == cur, ct);
    }

    public Task AddAsync(ProviderBalanceEntity entity, CancellationToken ct = default)
        => _db.ProviderBalances.AddAsync(entity, ct).AsTask();

    public void Update(ProviderBalanceEntity entity) => _db.ProviderBalances.Update(entity);
    public Task SaveChangesConcurrencySafeAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>BE-P10 §7.5 — persisted refund allocations.</summary>
public sealed class RefundAllocationRepository : IRefundAllocationRepository
{
    private readonly PaymentDbContext _db;
    public RefundAllocationRepository(PaymentDbContext db) => _db = db;

    public Task AddAsync(RefundAllocationEntity entity, CancellationToken ct = default)
        => _db.RefundAllocations.AddAsync(entity, ct).AsTask();

    public Task<RefundAllocationEntity?> GetByRefundRecordIdAsync(long refundRecordId, CancellationToken ct = default)
        => _db.RefundAllocations.FirstOrDefaultAsync(x => x.RefundRecordId == refundRecordId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

/// <summary>BE-P10 §21.2 — chargeback records (idempotent on the gateway reference).</summary>
public sealed class ChargebackRecordRepository : IChargebackRecordRepository
{
    private readonly PaymentDbContext _db;
    public ChargebackRecordRepository(PaymentDbContext db) => _db = db;

    public Task<ChargebackRecordEntity?> GetByGatewayReferenceAsync(string gatewayChargebackReference, CancellationToken ct = default)
        => _db.ChargebackRecords.FirstOrDefaultAsync(x => x.GatewayChargebackReference == gatewayChargebackReference, ct);

    public Task<ChargebackRecordEntity?> GetByTransactionIdAsync(long paymentTransactionId, CancellationToken ct = default)
        => _db.ChargebackRecords.AsNoTracking()
            .OrderByDescending(x => x.ReceivedAtUtc)
            .FirstOrDefaultAsync(x => x.PaymentTransactionId == paymentTransactionId, ct);

    public Task AddAsync(ChargebackRecordEntity entity, CancellationToken ct = default)
        => _db.ChargebackRecords.AddAsync(entity, ct).AsTask();

    public void Update(ChargebackRecordEntity entity) => _db.ChargebackRecords.Update(entity);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
