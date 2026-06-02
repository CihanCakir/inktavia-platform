using Aizen.Modules.ReferenceData.Repository.Mappings;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Abstraction.Request.Currency;
using Aizen.Modules.ReferenceData.Domain.Entities.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Context;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class CurrencyReferenceService : ICurrencyReferenceService
{
    private readonly ICurrencyRepository _repo;
    private readonly ReferenceDataDbContext _dbContext;

    public CurrencyReferenceService(ICurrencyRepository repo, ReferenceDataDbContext dbContext)
    {
        _repo = repo;
        _dbContext = dbContext;
    }

    public async Task<CurrencyDto> CreateAsync(CreateCurrencyRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _repo.ExistsByCodeAsync(request.Code, cancellationToken);
        if (exists) throw new AizenBusinessException($"Currency with code '{request.Code}' already exists.");

        if (request.IsBaseCurrency)
        {
            var existing = await _repo.GetBaseCurrencyAsync(cancellationToken);
            existing?.UnmarkAsBaseCurrency();
            if (existing != null) _repo.Update(existing);
        }

        var entity = CurrencyEntity.Create(request.Code, request.NumericCode, request.Name, request.Symbol, request.DecimalPlaces, request.IsBaseCurrency);
        await _repo.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<CurrencyDto> UpdateAsync(long id, string name, string symbol, int decimalPlaces, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken)
            ?? throw new AizenBusinessException($"Currency with id '{id}' not found.");
        entity.Update(name, symbol, decimalPlaces, isActive);
        _repo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task ActivateAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken)
            ?? throw new AizenBusinessException($"Currency with id '{id}' not found.");
        entity.Activate();
        _repo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken)
            ?? throw new AizenBusinessException($"Currency with id '{id}' not found.");
        if (entity.IsBaseCurrency) throw new AizenBusinessException("Cannot deactivate the base currency.");
        entity.Deactivate();
        _repo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CurrencyDto> SetBaseCurrencyAsync(long id, CancellationToken cancellationToken = default)
    {
        var existing = await _repo.GetBaseCurrencyAsync(cancellationToken);
        if (existing != null && existing.Id != id)
        {
            existing.UnmarkAsBaseCurrency();
            _repo.Update(existing);
        }

        var entity = await _repo.GetByIdAsync(id, cancellationToken)
            ?? throw new AizenBusinessException($"Currency with id '{id}' not found.");
        entity.MarkAsBaseCurrency();
        _repo.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<IReadOnlyList<CurrencyDto>> GetListAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var list = await _repo.GetListAsync(onlyActive, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<CurrencyDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken);
        return entity?.ToDto();
    }

    public async Task<CurrencyDto?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetBaseCurrencyAsync(cancellationToken);
        return entity?.ToDto();
    }
}
