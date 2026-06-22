using System.Text.Json;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Vessel.Repository.Seed.MockData;

/// <summary>
/// Idempotent mock data seeder for the Vessel module.
/// Seeds vessels, owners, specifications and engines using stable IDs for local and development environments only.
/// </summary>
[DocumentationInfo("Vessel mock data seeder", "Seeds admin-demo vessel data idempotently for local and development environments.")]
public sealed class VesselMockDataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly VesselDbContext _db;
    private readonly MockDataSeedOptions _options;
    private readonly ILogger<VesselMockDataSeeder> _logger;
    private readonly string _environment;

    public VesselMockDataSeeder(
        VesselDbContext db,
        IOptions<MockDataSeedOptions> options,
        ILogger<VesselMockDataSeeder> logger,
        string environment)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
        _environment = environment;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled || !_options.RunOnStartup)
        {
            _logger.LogDebug("Vessel MockData seeder is disabled.");
            return;
        }

        if (!_options.EnvironmentGuard.Contains(_environment, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Vessel MockData seeder skipped: environment '{Environment}' is not in EnvironmentGuard {Guard}.",
                _environment, string.Join(", ", _options.EnvironmentGuard));
            return;
        }

        _logger.LogInformation("Vessel MockData seeder starting (dataset: {DataSet}).", _options.DataSet);

        var basePath = Path.Combine(AppContext.BaseDirectory, "Seed", "Json", "MockData", _options.DataSet);

        await SeedVesselsAsync(basePath, ct);
        await SeedOwnersAsync(basePath, ct);
        await SeedSpecificationsAsync(basePath, ct);
        await SeedEnginesAsync(basePath, ct);
        await SeedMediaAsync(basePath, ct);
        await SeedDocumentsAsync(basePath, ct);
        await SeedLocationSnapshotsAsync(basePath, ct);
        await UpdateVesselClassificationAsync(basePath, ct);
        await AdvanceSequencesAsync(ct);

        _logger.LogInformation("Vessel MockData seeder completed.");
    }

    private async Task SeedVesselsAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "vessels.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Vessels seed file not found: {Path}", filePath);
            return;
        }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockVesselSeedModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.Vessels.AnyAsync(v => v.Id == model.Id, ct))
                continue;

            var vessel = VesselEntity.Create(
                vesselCode: model.VesselCode,
                name: model.Name,
                slug: model.Slug,
                vesselTypeCode: model.VesselTypeCode,
                description: model.Description,
                vesselUsageTypeCode: null,
                flagCountryCode: model.FlagCountryCode,
                registrationNumber: null,
                mmsiNumber: null,
                imoNumber: null,
                callSign: null,
                homeCountryCode: model.HomeCountryCode,
                homeCityCode: model.HomeCityCode,
                homeDistrictCode: null,
                homeMarinaName: model.HomeMarinaName,
                visibility: (VesselVisibility)model.Visibility);

            vessel.Id = model.Id;
            vessel.ChangeStatus((VesselStatus)model.Status);
            vessel.UpdateOperationalStatus(model.OperationalStatus);
            vessel.UpdateAssetType(model.AssetType);
            vessel.CreateDate = DateTime.UtcNow;
            vessel.ModifyDate = DateTime.UtcNow;
            vessel.IsDeleted = false;

            _db.Vessels.Add(vessel);

            try
            {
                await _db.SaveChangesAsync(ct);
                _logger.LogDebug("Seeded vessel {Id} ({Name}).", model.Id, model.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to seed vessel {Id} ({Name}), skipping.", model.Id, model.Name);
                _db.ChangeTracker.Clear();
            }
        }
    }

    private async Task SeedOwnersAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "vessel-owners.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Vessel owners seed file not found: {Path}", filePath);
            return;
        }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockVesselOwnerSeedModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.VesselOwners.AnyAsync(o => o.Id == model.Id, ct))
                continue;

            var owner = VesselOwnerEntity.Create(
                vesselId: model.VesselId,
                userId: model.UserId,
                userProfileId: model.UserProfileId,
                role: (VesselOwnershipRole)model.Role,
                isPrimary: model.IsPrimary);

            owner.Id = model.Id;
            owner.CreateDate = DateTime.UtcNow;
            owner.ModifyDate = DateTime.UtcNow;
            owner.IsDeleted = false;

            _db.VesselOwners.Add(owner);

            try
            {
                await _db.SaveChangesAsync(ct);
                _logger.LogDebug("Seeded vessel owner {Id} (vessel {VesselId}, user {UserId}).", model.Id, model.VesselId, model.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to seed vessel owner {Id}, skipping.", model.Id);
                _db.ChangeTracker.Clear();
            }
        }
    }

    private async Task SeedSpecificationsAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "vessel-specifications.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Vessel specifications seed file not found: {Path}", filePath);
            return;
        }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockVesselSpecSeedModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.VesselSpecifications.AnyAsync(s => s.Id == model.Id, ct))
                continue;

            var spec = VesselSpecificationEntity.Create(
                vesselId: model.VesselId,
                brand: model.Brand, model: model.Model, productionYear: model.ProductionYear,
                lengthValue: model.LengthValue, lengthUnitCode: model.LengthUnitCode,
                beamValue: model.BeamValue, beamUnitCode: model.BeamUnitCode,
                draftValue: model.DraftValue, draftUnitCode: model.DraftUnitCode,
                weightValue: model.WeightValue, weightUnitCode: model.WeightUnitCode,
                cabinCount: model.CabinCount, bedCount: model.BedCount, bathroomCount: model.BathroomCount,
                hullMaterialCode: model.HullMaterialCode,
                fuelCapacityValue: model.FuelCapacityValue, fuelCapacityUnitCode: model.FuelCapacityUnitCode,
                waterCapacityValue: model.WaterCapacityValue, waterCapacityUnitCode: model.WaterCapacityUnitCode);

            spec.Id = model.Id;
            spec.CreateDate = DateTime.UtcNow;
            spec.ModifyDate = DateTime.UtcNow;
            spec.IsDeleted = false;

            _db.VesselSpecifications.Add(spec);

            try
            {
                await _db.SaveChangesAsync(ct);
                _logger.LogDebug("Seeded vessel specification {Id} for vessel {VesselId}.", model.Id, model.VesselId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to seed vessel specification {Id}, skipping.", model.Id);
                _db.ChangeTracker.Clear();
            }
        }
    }

    private async Task SeedEnginesAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "vessel-engines.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Vessel engines seed file not found: {Path}", filePath);
            return;
        }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockVesselEngineSeedModel>>(stream, JsonOptions, ct) ?? [];

        foreach (var model in models)
        {
            if (await _db.VesselEngines.AnyAsync(e => e.Id == model.Id, ct))
                continue;

            var engine = VesselEngineEntity.Create(
                vesselId: model.VesselId,
                engineName: model.EngineName,
                engineTypeCode: model.EngineTypeCode,
                fuelTypeCode: model.FuelTypeCode,
                brand: model.Brand,
                model: model.Model,
                serialNumber: model.SerialNumber,
                horsePower: model.HorsePower,
                productionYear: model.ProductionYear,
                isPrimary: model.IsPrimary);

            engine.Id = model.Id;
            engine.CreateDate = DateTime.UtcNow;
            engine.ModifyDate = DateTime.UtcNow;
            engine.IsDeleted = false;

            _db.VesselEngines.Add(engine);

            try
            {
                await _db.SaveChangesAsync(ct);
                _logger.LogDebug("Seeded vessel engine {Id} for vessel {VesselId}.", model.Id, model.VesselId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to seed vessel engine {Id}, skipping.", model.Id);
                _db.ChangeTracker.Clear();
            }
        }
    }

    private async Task SeedMediaAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "vessel-media.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Vessel media seed file not found: {Path}", filePath);
            return;
        }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockVesselMediaSeedModel>>(stream, JsonOptions, ct) ?? [];

        int inserted = 0, skipped = 0;
        foreach (var model in models)
        {
            if (await _db.VesselMedia.AnyAsync(m => m.Id == model.Id, ct))
            {
                skipped++;
                continue;
            }

            var media = VesselMediaEntity.Create(
                vesselId: model.VesselId,
                mediaType: (VesselMediaType)model.MediaType,
                fileId: null,
                originalFileNameSnapshot: model.OriginalFileNameSnapshot,
                contentTypeSnapshot: model.ContentTypeSnapshot,
                sizeInBytesSnapshot: model.SizeInBytesSnapshot,
                sortOrder: model.SortOrder,
                isCover: model.IsCover);

            media.Id = model.Id;
            media.CreateDate = DateTime.UtcNow;
            media.ModifyDate = DateTime.UtcNow;
            media.IsDeleted = false;

            _db.VesselMedia.Add(media);

            try
            {
                await _db.SaveChangesAsync(ct);
                inserted++;
                _logger.LogDebug("Seeded vessel media {Id} for vessel {VesselId}.", model.Id, model.VesselId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to seed vessel media {Id}, skipping.", model.Id);
                _db.ChangeTracker.Clear();
            }
        }

        _logger.LogInformation("[VesselDemoSeed] Inserted media: {Inserted}, skipped: {Skipped}", inserted, skipped);
    }

    private async Task SeedDocumentsAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "vessel-documents.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Vessel documents seed file not found: {Path}", filePath);
            return;
        }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockVesselDocumentSeedModel>>(stream, JsonOptions, ct) ?? [];

        int inserted = 0, skipped = 0;
        foreach (var model in models)
        {
            if (await _db.VesselDocuments.AnyAsync(d => d.Id == model.Id, ct))
            {
                skipped++;
                continue;
            }

            var doc = VesselDocumentEntity.Create(
                vesselId: model.VesselId,
                documentTypeCode: model.DocumentTypeCode,
                documentName: model.DocumentName,
                fileId: null,
                originalFileNameSnapshot: null,
                contentTypeSnapshot: null,
                sizeInBytesSnapshot: null,
                expiresAt: model.ExpiresAt,
                notes: model.Notes);

            doc.Id = model.Id;
            doc.ChangeStatus((VesselDocumentStatus)model.DocumentStatus);
            doc.CreateDate = DateTime.UtcNow;
            doc.ModifyDate = DateTime.UtcNow;
            doc.IsDeleted = false;

            _db.VesselDocuments.Add(doc);

            try
            {
                await _db.SaveChangesAsync(ct);
                inserted++;
                _logger.LogDebug("Seeded vessel document {Id} for vessel {VesselId}.", model.Id, model.VesselId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to seed vessel document {Id}, skipping.", model.Id);
                _db.ChangeTracker.Clear();
            }
        }

        _logger.LogInformation("[VesselDemoSeed] Inserted documents: {Inserted}, skipped: {Skipped}", inserted, skipped);
    }

    private async Task SeedLocationSnapshotsAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "vessel-location-snapshots.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Vessel location snapshots seed file not found: {Path}", filePath);
            return;
        }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockVesselLocationSnapshotSeedModel>>(stream, JsonOptions, ct) ?? [];

        int inserted = 0, skipped = 0;
        foreach (var model in models)
        {
            if (await _db.VesselLocationSnapshots.AnyAsync(l => l.Id == model.Id, ct))
            {
                skipped++;
                continue;
            }

            var snapshot = VesselLocationSnapshotEntity.Create(
                vesselId: model.VesselId,
                countryCode: model.CountryCode,
                cityCode: model.CityCode,
                districtCode: null,
                marinaName: model.MarinaName,
                latitude: model.Latitude,
                longitude: model.Longitude,
                accuracyMeters: model.AccuracyMeters,
                source: model.Source,
                capturedAt: model.CapturedAt);

            snapshot.Id = model.Id;
            snapshot.CreateDate = DateTime.UtcNow;
            snapshot.ModifyDate = DateTime.UtcNow;
            snapshot.IsDeleted = false;

            _db.VesselLocationSnapshots.Add(snapshot);

            try
            {
                await _db.SaveChangesAsync(ct);
                inserted++;
                _logger.LogDebug("Seeded vessel location snapshot {Id} for vessel {VesselId}.", model.Id, model.VesselId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to seed vessel location snapshot {Id}, skipping.", model.Id);
                _db.ChangeTracker.Clear();
            }
        }

        _logger.LogInformation("[VesselDemoSeed] Inserted location snapshots: {Inserted}, skipped: {Skipped}", inserted, skipped);
    }

    private async Task UpdateVesselClassificationAsync(string basePath, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "vessels.json");
        if (!File.Exists(filePath)) return;

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockVesselSeedModel>>(stream, JsonOptions, ct) ?? [];

        int updated = 0;
        foreach (var model in models)
        {
            if (model.AssetType == null && model.OperationalStatus == null)
                continue;

            var vessel = await _db.Vessels.FindAsync(new object[] { model.Id }, ct);
            if (vessel == null)
                continue;

            bool changed = false;

            if (model.AssetType != null && vessel.AssetType != model.AssetType)
            {
                vessel.UpdateAssetType(model.AssetType);
                changed = true;
            }

            if (model.OperationalStatus != null && vessel.OperationalStatus != model.OperationalStatus)
            {
                vessel.UpdateOperationalStatus(model.OperationalStatus);
                changed = true;
            }

            if (!changed)
                continue;

            try
            {
                await _db.SaveChangesAsync(ct);
                updated++;
                _logger.LogDebug("Updated vessel {Id} classification: AssetType={AssetType}, OperationalStatus={OperationalStatus}.",
                    model.Id, model.AssetType, model.OperationalStatus);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update classification for vessel {Id}, skipping.", model.Id);
                _db.ChangeTracker.Clear();
            }
        }

        _logger.LogInformation("[VesselDemoSeed] Updated vessel classification for {Updated} vessels.", updated);
    }

    private async Task AdvanceSequencesAsync(CancellationToken ct)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync(@"
                DO $$
                DECLARE seq_name text;
                BEGIN
                    SELECT pg_get_serial_sequence('vessel.vessels', 'Id') INTO seq_name;
                    IF seq_name IS NOT NULL THEN
                        PERFORM setval(seq_name, GREATEST(100000, COALESCE((SELECT MAX(""Id"") FROM vessel.vessels), 0)));
                    END IF;
                    
                    SELECT pg_get_serial_sequence('vessel.vessel_owners', 'Id') INTO seq_name;
                    IF seq_name IS NOT NULL THEN
                        PERFORM setval(seq_name, GREATEST(100000, COALESCE((SELECT MAX(""Id"") FROM vessel.vessel_owners), 0)));
                    END IF;
                    
                    SELECT pg_get_serial_sequence('vessel.vessel_specifications', 'Id') INTO seq_name;
                    IF seq_name IS NOT NULL THEN
                        PERFORM setval(seq_name, GREATEST(100000, COALESCE((SELECT MAX(""Id"") FROM vessel.vessel_specifications), 0)));
                    END IF;
                    
                    SELECT pg_get_serial_sequence('vessel.vessel_engines', 'Id') INTO seq_name;
                    IF seq_name IS NOT NULL THEN
                        PERFORM setval(seq_name, GREATEST(100000, COALESCE((SELECT MAX(""Id"") FROM vessel.vessel_engines), 0)));
                    END IF;

                    SELECT pg_get_serial_sequence('vessel.vessel_media', 'Id') INTO seq_name;
                    IF seq_name IS NOT NULL THEN
                        PERFORM setval(seq_name, GREATEST(100000, COALESCE((SELECT MAX(""Id"") FROM vessel.vessel_media), 0)));
                    END IF;

                    SELECT pg_get_serial_sequence('vessel.vessel_documents', 'Id') INTO seq_name;
                    IF seq_name IS NOT NULL THEN
                        PERFORM setval(seq_name, GREATEST(100000, COALESCE((SELECT MAX(""Id"") FROM vessel.vessel_documents), 0)));
                    END IF;

                    SELECT pg_get_serial_sequence('vessel.vessel_location_snapshots', 'Id') INTO seq_name;
                    IF seq_name IS NOT NULL THEN
                        PERFORM setval(seq_name, GREATEST(100000, COALESCE((SELECT MAX(""Id"") FROM vessel.vessel_location_snapshots), 0)));
                    END IF;
                END $$;", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not advance Vessel sequences (non-fatal).");
        }
    }

    // --- Seed models ---

    private sealed record MockVesselSeedModel(
        long Id, string VesselCode, string Name, string Slug,
        string VesselTypeCode, int Status, int Visibility,
        string? HomeCountryCode, string? HomeCityCode, string? HomeMarinaName,
        string? FlagCountryCode, string? Description,
        int? AssetType, int? OperationalStatus);

    private sealed record MockVesselOwnerSeedModel(
        long Id, long VesselId, long UserId, long? UserProfileId,
        int Role, int OwnershipStatus, bool IsPrimary);

    private sealed record MockVesselSpecSeedModel(
        long Id, long VesselId,
        string? Brand, string? Model, int? ProductionYear,
        decimal? LengthValue, string? LengthUnitCode,
        decimal? BeamValue, string? BeamUnitCode,
        decimal? DraftValue, string? DraftUnitCode,
        decimal? WeightValue, string? WeightUnitCode,
        int? CabinCount, int? BedCount, int? BathroomCount,
        string? HullMaterialCode,
        decimal? FuelCapacityValue, string? FuelCapacityUnitCode,
        decimal? WaterCapacityValue, string? WaterCapacityUnitCode);

    private sealed record MockVesselEngineSeedModel(
        long Id, long VesselId,
        string EngineName, string EngineTypeCode, string FuelTypeCode,
        string? Brand, string? Model, string? SerialNumber,
        int? HorsePower, int? ProductionYear, bool IsPrimary);

    private sealed record MockVesselMediaSeedModel(
        long Id, long VesselId,
        int MediaType,
        string? OriginalFileNameSnapshot,
        string? ContentTypeSnapshot,
        long? SizeInBytesSnapshot,
        int SortOrder,
        bool IsCover);

    private sealed record MockVesselDocumentSeedModel(
        long Id, long VesselId,
        string DocumentTypeCode,
        string DocumentName,
        DateTime? ExpiresAt,
        int DocumentStatus,
        string? Notes,
        string? IssuingAuthority);

    private sealed record MockVesselLocationSnapshotSeedModel(
        long Id, long VesselId,
        string? CountryCode, string? CityCode, string? MarinaName,
        decimal? Latitude, decimal? Longitude, decimal? AccuracyMeters,
        string? Source, DateTime CapturedAt, bool IsCurrent);
}
