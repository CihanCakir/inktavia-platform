using Aizen.Modules.ReferenceData.Domain.Entities.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ReferenceData.Repository.Repositories.Currency;

public sealed class CurrencyRepository : ICurrencyRepository
{
    private readonly ReferenceDataDbContext _dbContext;

    public CurrencyRepository(ReferenceDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CurrencyEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => _dbContext.Currencies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<CurrencyEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.Currencies.FirstOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);
    }

    public Task<CurrencyEntity?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default)
        => _dbContext.Currencies.FirstOrDefaultAsync(x => x.IsBaseCurrency && x.IsActive, cancellationToken);

    public async Task<IReadOnlyList<CurrencyEntity>> GetListAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Currencies.AsNoTracking();
        if (onlyActive) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.Currencies.AnyAsync(x => x.Code == normalizedCode, cancellationToken);
    }

    public Task AddAsync(CurrencyEntity entity, CancellationToken cancellationToken = default)
        => _dbContext.Currencies.AddAsync(entity, cancellationToken).AsTask();

    public void Update(CurrencyEntity entity)
        => _dbContext.Currencies.Update(entity);
}
