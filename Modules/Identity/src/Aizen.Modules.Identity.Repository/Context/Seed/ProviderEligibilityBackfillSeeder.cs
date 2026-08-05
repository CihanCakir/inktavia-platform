using System.Text.Json;
using Aizen.Core.Domain;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities.ProviderServiceCategory;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Identity.Repository.Context.Seed;

/// <summary>
/// I2 idempotent backfill — for existing approved+active providers, populate <c>UserProfileEntity.City</c> and the
/// new <c>provider_service_categories</c> rows from their onboarding <c>DraftJson</c> (OperatingRegion.cityCode +
/// ServiceCapabilities.selectedServiceCategoryIds). Re-runnable: only fills an empty City and only inserts categories
/// when a provider has none. Providers with no resolvable city are skipped and counted (they get no area notifications
/// until they set a city). Runs at boot after migrations. Reports counts.
/// </summary>
public sealed class ProviderEligibilityBackfillSeeder
{
    private readonly IdentityDbContext _db;
    private readonly ILogger<ProviderEligibilityBackfillSeeder> _logger;

    public ProviderEligibilityBackfillSeeder(IdentityDbContext db, ILogger<ProviderEligibilityBackfillSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var providers = await _db.UserProfiles
            .Where(p => p.RoleContext == WorkshopRoleContext.Organizer
                     && p.ApprovalStatus == ApprovalStatus.Approved
                     && p.Status == ProfileStatus.Active
                     && !p.IsDeleted)
            .ToListAsync(ct);

        int citiesPopulated = 0, categoryProviders = 0, skippedNoCity = 0, alreadyOk = 0;

        foreach (var profile in providers)
        {
            var onboarding = await _db.ProviderOnboarding
                .FirstOrDefaultAsync(o => o.ProfileId == profile.Id && !o.IsDeleted, ct);

            var draft = onboarding?.GetDraft() ?? new Dictionary<string, JsonElement>();

            // ── City ──────────────────────────────────────────────────────────────
            var cityMissing = string.IsNullOrWhiteSpace(profile.City);
            if (cityMissing)
            {
                var cityCode = TryGetCity(draft);
                if (!string.IsNullOrWhiteSpace(cityCode))
                {
                    profile.SetLocation(cityCode!.Trim().ToUpperInvariant(),
                        profile.Country ?? TryGetCountry(draft)?.Trim().ToUpperInvariant() ?? "TR");
                    _db.UserProfiles.Update(profile);
                    citiesPopulated++;
                }
                else
                {
                    skippedNoCity++;
                    _logger.LogWarning(
                        "I2 backfill: provider profile {ProfileId} has no resolvable city (empty onboarding draft) — " +
                        "skipped; will not receive area notifications until a city is set.", profile.Id);
                }
            }
            else
            {
                alreadyOk++;
            }

            // ── Categories (only if the provider has none yet) ─────────────────────
            var hasCategories = await _db.ProviderServiceCategories.AnyAsync(c => c.ProfileId == profile.Id, ct);
            if (!hasCategories)
            {
                var codes = TryGetCategoryCodes(draft);
                if (codes.Count > 0)
                {
                    foreach (var code in codes.Select(c => c.Trim().ToLowerInvariant()).Distinct())
                        await _db.ProviderServiceCategories.AddAsync(
                            ProviderServiceCategoryEntity.Create(profile.Id, profile.UserId, code), ct);
                    categoryProviders++;
                }
            }
        }

        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "I2 provider-eligibility backfill: {Total} approved providers scanned — city populated for {Cities}, " +
            "categories populated for {Cats} providers, {AlreadyOk} already had a city, {Skipped} skipped (no city in draft).",
            providers.Count, citiesPopulated, categoryProviders, alreadyOk, skippedNoCity);
    }

    private static string? TryGetCity(Dictionary<string, JsonElement> draft)
        => draft.TryGetValue("OperatingRegion", out var or) && or.ValueKind == JsonValueKind.Object
           && or.TryGetProperty("cityCode", out var cc) && cc.ValueKind == JsonValueKind.String
            ? cc.GetString()
            : null;

    private static string? TryGetCountry(Dictionary<string, JsonElement> draft)
        => draft.TryGetValue("OperatingRegion", out var or) && or.ValueKind == JsonValueKind.Object
           && or.TryGetProperty("country", out var co) && co.ValueKind == JsonValueKind.String
            ? co.GetString()
            : null;

    private static List<string> TryGetCategoryCodes(Dictionary<string, JsonElement> draft)
    {
        var result = new List<string>();
        if (draft.TryGetValue("ServiceCapabilities", out var sc) && sc.ValueKind == JsonValueKind.Object
            && sc.TryGetProperty("selectedServiceCategoryIds", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
                if (el.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(el.GetString()))
                    result.Add(el.GetString()!);
        }
        return result;
    }
}
