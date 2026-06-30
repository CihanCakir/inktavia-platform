using Aizen.Modules.ReferenceData.Domain.Entities.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Seed.Models.Currency;
using Aizen.Modules.ReferenceData.Repository.Seed.Readers;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Services;

/// <summary>Seeds currency entities from JSON files.</summary>
[DocumentationInfo(
    "Seeds CurrencyEntity records from currencies.json.",
    "Idempotency key: Code. Uses ICurrencyRepository to check existence and add/update.")]
public sealed class CurrencyJsonSeedService
{
    private readonly ICurrencyRepository _currencyRepository;
    private readonly ReferenceDataDbContext _dbContext;
    private readonly IReferenceDataJsonSeedReader _reader;

    public CurrencyJsonSeedService(
        ICurrencyRepository currencyRepository,
        ReferenceDataDbContext dbContext,
        IReferenceDataJsonSeedReader reader)
    {
        _currencyRepository = currencyRepository;
        _dbContext = dbContext;
        _reader = reader;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Currencies.AnyAsync(cancellationToken)) return;

        var models = await _reader.ReadListAsync<CurrencySeedModel>("Currency/currencies.json", cancellationToken: cancellationToken);

        foreach (var model in models)
        {
            var existing = await _currencyRepository.GetByCodeAsync(model.Code, cancellationToken);
            if (existing is null)
            {
                var entity = CurrencyEntity.Create(
                    model.Code,
                    model.NumericCode,
                    model.Name,
                    model.Symbol,
                    model.DecimalPlaces,
                    model.IsBaseCurrency);

                if (!model.IsActive) entity.Deactivate();

                await _currencyRepository.AddAsync(entity, cancellationToken);
            }
            else
            {
                existing.Update(model.Name, model.Symbol, model.DecimalPlaces, model.IsActive);
                if (model.IsBaseCurrency) existing.MarkAsBaseCurrency();
                _currencyRepository.Update(existing);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
