using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Repository.Seed;

/// <summary>
/// S2a — idempotent boot seed of the marine pricing attribute definitions (EngineInstallationType, PaintType,
/// WorkDifficulty) mapped to the R4 lookup groups and scoped to the relevant ServiceRequest categories. Dedupe-by-code:
/// a definition already present (by Code) is left untouched, so admin edits are never clobbered and re-runs are no-ops.
/// </summary>
public sealed class PricingAttributeDefinitionSeeder
{
    private readonly ServiceRequestDbContext _db;
    private readonly ILogger<PricingAttributeDefinitionSeeder> _logger;

    public PricingAttributeDefinitionSeeder(ServiceRequestDbContext db, ILogger<PricingAttributeDefinitionSeeder> logger)
    { _db = db; _logger = logger; }

    private sealed record SeedDef(
        string Code, string NameTr, string NameEn, string LookupGroupCode, bool IsRequired, int SortOrder,
        string[] Categories);

    private static readonly SeedDef[] Definitions =
    {
        new("ENGINE_INSTALLATION_TYPE", "Motor Kurulum Tipi", "Engine Installation Type", "ENGINE_INSTALLATION_TYPE",
            IsRequired: false, SortOrder: 10, new[] { "MAINTENANCE", "REPAIR", "INSPECTION" }),
        new("PAINT_TYPE", "Boya Tipi", "Paint Type", "PAINT_TYPE",
            IsRequired: false, SortOrder: 20, new[] { "PAINTING", "MAINTENANCE" }),
        new("WORK_DIFFICULTY", "İş Zorluğu", "Work Difficulty", "WORK_DIFFICULTY",
            IsRequired: false, SortOrder: 30, new[] { "PAINTING", "MAINTENANCE", "REPAIR", "INSPECTION", "CLEANING", "TREATMENT" }),
    };

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var existing = await _db.PricingAttributeDefinitions.Select(d => d.Code).ToListAsync(ct);
        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var def in Definitions)
        {
            if (existingSet.Contains(def.Code)) continue;   // dedupe by code — additive only
            var entity = PricingAttributeDefinitionEntity.Create(
                def.Code, def.NameTr, def.NameEn, PricingAttributeDataType.Lookup, def.LookupGroupCode,
                def.IsRequired, def.SortOrder, minValue: null, maxValue: null, def.Categories);
            _db.PricingAttributeDefinitions.Add(entity);
            added++;
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} marine pricing attribute definitions.", added);
        }
    }
}
