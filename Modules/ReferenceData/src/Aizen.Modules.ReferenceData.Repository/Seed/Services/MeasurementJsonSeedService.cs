using Aizen.Modules.ReferenceData.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Abstraction.Model;
using Aizen.Modules.ReferenceData.Domain.Entities.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Seed.Models.Measurement;
using Aizen.Modules.ReferenceData.Repository.Seed.Readers;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Services;

/// <summary>Seeds measurement unit entities from JSON files.</summary>
[DocumentationInfo(
    "Seeds MeasurementUnitEntity records from measurement-units.json.",
    "Idempotency key: Code.")]
public sealed class MeasurementJsonSeedService
{
    private readonly IMeasurementUnitRepository _repository;
    private readonly ReferenceDataDbContext _dbContext;
    private readonly IReferenceDataJsonSeedReader _reader;

    public MeasurementJsonSeedService(
        IMeasurementUnitRepository repository,
        ReferenceDataDbContext dbContext,
        IReferenceDataJsonSeedReader reader)
    {
        _repository = repository;
        _dbContext = dbContext;
        _reader = reader;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.MeasurementUnits.AnyAsync(cancellationToken)) return;

        var models = await _reader.ReadListAsync<MeasurementUnitSeedModel>("Measurement/measurement-units.json", cancellationToken: cancellationToken);

        foreach (var model in models)
        {
            var unitType = (MeasurementUnitType)model.UnitType;
            var existing = await _repository.GetByCodeAsync(model.Code, cancellationToken);

            if (existing is null)
            {
                var entity = MeasurementUnitEntity.Create(
                    model.Code,
                    model.Name,
                    model.Symbol,
                    unitType,
                    model.ConversionFactorToBase,
                    model.BaseUnitCode);

                if (!model.IsActive) entity.Deactivate();

                await _repository.AddAsync(entity, cancellationToken);
            }
            else
            {
                existing.Update(model.Name, model.Symbol, model.ConversionFactorToBase, model.BaseUnitCode, model.IsActive);
                _repository.Update(existing);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
