using Aizen.Modules.ReferenceData.Domain.Entities.Marina;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Seed.Models.Marina;
using Aizen.Modules.ReferenceData.Repository.Seed.Readers;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Services;

/// <summary>Seeds marina reference entities from JSON files.</summary>
[DocumentationInfo(
    "Seeds MarinaEntity records from Marina/marinas.json.",
    "Idempotency key: Code. NB: per-key upsert — deliberately NO whole-table AnyAsync short-circuit, so newly-added marinas seed even on an already-populated DB.")]
public sealed class MarinaJsonSeedService
{
    private readonly IMarinaRepository _marinaRepository;
    private readonly ReferenceDataDbContext _dbContext;
    private readonly IReferenceDataJsonSeedReader _reader;

    public MarinaJsonSeedService(
        IMarinaRepository marinaRepository,
        ReferenceDataDbContext dbContext,
        IReferenceDataJsonSeedReader reader)
    {
        _marinaRepository = marinaRepository;
        _dbContext = dbContext;
        _reader = reader;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var models = await _reader.ReadListAsync<MarinaSeedModel>("Marina/marinas.json", optional: true, cancellationToken: cancellationToken);
        if (models.Count == 0) return;

        foreach (var model in models)
        {
            var existing = await _marinaRepository.GetByCodeAsync(model.Code, cancellationToken);
            if (existing is null)
            {
                var entity = MarinaEntity.Create(
                    model.Code, model.Name, model.Type, model.CountryCode, model.CityCode,
                    model.Province, model.District, model.Latitude, model.Longitude,
                    model.OsmId, model.NeedsReview);
                if (!model.IsActive) entity.Deactivate();
                await _marinaRepository.AddAsync(entity, cancellationToken);
            }
            else
            {
                // CURATION GUARD: once an admin has touched this row (edit / mark-reviewed / deactivate), never
                // clobber its fields from JSON again. New rows still seed (the insert branch above); only the
                // field-overwrite is skipped for admin-edited rows.
                if (existing.IsAdminEdited) continue;

                existing.Update(
                    model.Name, model.Type, model.CountryCode, model.CityCode, model.Province, model.District,
                    model.Latitude, model.Longitude, model.OsmId, model.NeedsReview, model.IsActive);
                _marinaRepository.Update(existing);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
