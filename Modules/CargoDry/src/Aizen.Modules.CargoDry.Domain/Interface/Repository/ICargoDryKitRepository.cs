using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDryKitRepository
{
    Task<CargoDryKitEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CargoDryKitEntity?> GetBySerialAsync(string serialNumber, CancellationToken ct = default);
    Task<List<CargoDryKitEntity>> GetByOwnerAsync(long userId, CancellationToken ct = default);
    Task<CargoDryKitEntity?> GetActiveByVesselAsync(long vesselId, string productCode, CancellationToken ct = default);
    Task<List<CargoDryKitEntity>> GetExpiringAsync(int withinDays, long? providerProfileId = null, CancellationToken ct = default);
    Task<List<CargoDryKitEntity>> GetExpiredUnmarkedAsync(long? providerProfileId = null, CancellationToken ct = default);
    Task<(List<CargoDryKitEntity> Items, int Total)> GetPagedAsync(
        CargoDryKitStatus? status, string? search, long? vesselId, long? ownerUserId, string? batchCode, int skip, int take, long? providerProfileId = null, CancellationToken ct = default);
    Task<List<CargoDryKitEntity>> GetAllAsync(CancellationToken ct = default);
    Task<List<CargoDryKitEntity>> GetAllForReportAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<CargoDryStatsProjection> GetStatsAsync(long? providerProfileId = null, CancellationToken ct = default);
    /// <summary>
    /// Returns all Available (un-activated) kits belonging to the given batch.
    /// Used by batch revoke to cascade-revoke un-used kits.
    /// </summary>
    Task<List<CargoDryKitEntity>> GetAvailableByBatchCodeAsync(string batchCode, CancellationToken ct = default);

    /// <summary>
    /// SQL-level aggregation of kit statistics for a single product.
    /// Use in place of <c>GetAllAsync</c> when only per-product counts are needed.
    /// </summary>
    Task<CargoDryProductKitStatsProjection> GetKitStatsByProductCodeAsync(
        string productCode, CancellationToken ct = default);

    /// <summary>
    /// Exact kit-code lookup. Returns null when no kit with the given code exists.
    /// Used by the admin lookup endpoint (Phase 8B).
    /// </summary>
    Task<CargoDryKitEntity?> GetByKitCodeAsync(string kitCode, CancellationToken ct = default);

    Task AddAsync(CargoDryKitEntity entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<CargoDryKitEntity> entities, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Per-product kit stats computed at SQL level — avoids full-table materialisation.
/// </summary>
public sealed class CargoDryProductKitStatsProjection
{
    public int    TotalKits            { get; init; }
    public int    ActiveKits           { get; init; }
    public int    ExpiredKits          { get; init; }
    public int    RevokedKits          { get; init; }
    public int    RenewedKits          { get; init; }
    public int    ExpiringIn30Days     { get; init; }
    public double AvgEfficiencyPercent { get; init; }
    public double RenewalRatePercent   { get; init; }
}

public sealed class CargoDryStatsProjection
{
    public int Total { get; init; }
    public int Available { get; init; }
    public int Active { get; init; }
    public int Expiring { get; init; }
    public int Expired { get; init; }
    public int Revoked { get; init; }
    public int TodayActivations { get; init; }
    public int WithRenewals { get; init; }
}
