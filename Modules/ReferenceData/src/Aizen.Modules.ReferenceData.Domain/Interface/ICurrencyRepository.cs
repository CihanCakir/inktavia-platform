using Aizen.Modules.ReferenceData.Domain.Entities.Currency;

namespace Aizen.Modules.ReferenceData.Domain.Interface;

public interface ICurrencyRepository
{
    Task<CurrencyEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<CurrencyEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<CurrencyEntity?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CurrencyEntity>> GetListAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(CurrencyEntity entity, CancellationToken cancellationToken = default);
    void Update(CurrencyEntity entity);
}
