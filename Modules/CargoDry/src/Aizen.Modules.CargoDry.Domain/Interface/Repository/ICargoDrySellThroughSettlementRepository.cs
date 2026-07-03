using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Domain.Interface.Repository;

public interface ICargoDrySellThroughSettlementRepository
{
    Task<CargoDrySellThroughSettlementEntity?> GetByIdAsync(long id, CancellationToken ct);

    Task<CargoDrySellThroughSettlementEntity?> GetByCodeAsync(string settlementCode, CancellationToken ct);

    /// <summary>
    /// Finds the most recent open (Pending) settlement for the given agreement + product.
    /// Used by ICargoDryCommercialActivationService to find an existing settlement to append to.
    /// </summary>
    Task<CargoDrySellThroughSettlementEntity?> GetOpenForAgreementProductAsync(
        long consignmentAgreementId, string productCode, CancellationToken ct);

    Task<(List<CargoDrySellThroughSettlementEntity> Items, int Total)> GetPagedAsync(
        long?                               providerProfileId,
        long?                               consignmentAgreementId,
        string?                             productCode,
        CargoDrySellThroughSettlementStatus? status,
        DateTime?                           periodFrom,
        DateTime?                           periodTo,
        string?                             search,
        int                                 skip,
        int                                 take,
        CancellationToken                   ct);

    Task AddAsync(CargoDrySellThroughSettlementEntity entity, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
