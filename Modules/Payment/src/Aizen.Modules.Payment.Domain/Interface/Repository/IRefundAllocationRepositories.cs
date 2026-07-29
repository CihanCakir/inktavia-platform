using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

/// <summary>BE-P10 — versioned refund-allocation policy repository (mirrors IProfitProtectionPolicyRepository).</summary>
public interface IRefundAllocationPolicyRepository
{
    Task<RefundAllocationPolicyEntity?> ResolveAsync(string currency, DateTime atUtc, CancellationToken ct = default);
    Task<RefundAllocationPolicyEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<RefundAllocationPolicyEntity>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(RefundAllocationPolicyEntity entity, CancellationToken ct = default);
    void Update(RefundAllocationPolicyEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>BE-P10 §7.4 — provider negative-balance ledger repository.</summary>
public interface IProviderBalanceRepository
{
    Task<ProviderBalanceEntity?> GetByProviderAsync(long providerProfileId, string currency, CancellationToken ct = default);
    Task AddAsync(ProviderBalanceEntity entity, CancellationToken ct = default);
    void Update(ProviderBalanceEntity entity);
    /// <summary>Concurrency-safe save (maps a Version clash appropriately).</summary>
    Task SaveChangesConcurrencySafeAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>BE-P10 §7.5 — persisted refund allocations.</summary>
public interface IRefundAllocationRepository
{
    Task AddAsync(RefundAllocationEntity entity, CancellationToken ct = default);
    Task<RefundAllocationEntity?> GetByRefundRecordIdAsync(long refundRecordId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>BE-P10 §21.2 — chargeback records (idempotent on the gateway reference).</summary>
public interface IChargebackRecordRepository
{
    Task<ChargebackRecordEntity?> GetByGatewayReferenceAsync(string gatewayChargebackReference, CancellationToken ct = default);
    Task AddAsync(ChargebackRecordEntity entity, CancellationToken ct = default);
    void Update(ChargebackRecordEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
