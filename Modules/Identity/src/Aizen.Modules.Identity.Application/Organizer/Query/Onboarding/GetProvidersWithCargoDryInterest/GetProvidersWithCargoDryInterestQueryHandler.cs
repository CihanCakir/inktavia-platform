using System.Text.Json;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Onboarding.GetProvidersWithCargoDryInterest;

public sealed class GetProvidersWithCargoDryInterestQueryHandler
    : AizenQueryHandler<GetProvidersWithCargoDryInterestQuery, CargoDryInterestApplicantsResult>
{
    private readonly IdentityDbContext _db;

    public GetProvidersWithCargoDryInterestQueryHandler(IdentityDbContext db) => _db = db;

    public override async Task<CargoDryInterestApplicantsResult?> Handle(
        GetProvidersWithCargoDryInterestQuery request, CancellationToken ct)
    {
        // The interest flag lives in the per-row `DraftJson` jsonb under DraftJson["CargoDryInterest"].interested, with
        // no column/index. Rather than a fragile jsonb-path LINQ translation, load the candidate rows (bounded — a modest
        // provider population, admin-only) projected to just the columns we need, and parse the flag in memory. If this
        // queue must scale/sort, denormalize an indexed `CargoDryInterested` bool on ProviderOnboardingEntity.SaveStep
        // (additive migration) and filter on it — follow-up, out of scope here.
        var candidates = await _db.ProviderOnboarding
            .AsNoTracking()
            .Where(x => x.DraftJson != null && x.DraftJson != "{}")
            .Select(x => new { x.ProfileId, x.UserId, x.Status, x.DraftJson, x.LastSavedAtUtc })
            .ToListAsync(ct);

        var applicants = new List<CargoDryInterestApplicantDto>();
        foreach (var c in candidates)
        {
            if (!TryReadInterest(c.DraftJson, out var commercialPref)) continue;
            applicants.Add(new CargoDryInterestApplicantDto
            {
                ProfileId                 = c.ProfileId,
                UserId                    = c.UserId,
                OnboardingStatus          = c.Status.ToString(),
                InterestDeclaredAtUtc     = c.LastSavedAtUtc,
                CommercialModelPreference = commercialPref,
            });
        }

        applicants = applicants.OrderByDescending(a => a.InterestDeclaredAtUtc).ToList();
        var total = applicants.Count;
        var paged = applicants.Skip(request.PageIndex * request.PageSize).Take(request.PageSize).ToList();

        return new CargoDryInterestApplicantsResult
        {
            Items = paged, Total = total, PageIndex = request.PageIndex, PageSize = request.PageSize,
        };
    }

    /// <summary>True when DraftJson["CargoDryInterest"].interested == true (case-insensitive). Out: the declared commercial-model preference if present.</summary>
    private static bool TryReadInterest(string draftJson, out string? commercialModelPreference)
    {
        commercialModelPreference = null;
        try
        {
            using var doc = JsonDocument.Parse(draftJson);
            if (!TryGetPropertyCI(doc.RootElement, "CargoDryInterest", out var step) || step.ValueKind != JsonValueKind.Object)
                return false;
            if (!TryGetPropertyCI(step, "interested", out var interested)
                || interested.ValueKind != JsonValueKind.True)
                return false;

            // FE-defined key for the commercial-model preference — accept a few likely names.
            foreach (var key in new[] { "commercialModelPreference", "commercialModel", "preferredCommercialModel", "commercialPreference" })
                if (TryGetPropertyCI(step, key, out var pref) && pref.ValueKind == JsonValueKind.String)
                {
                    commercialModelPreference = pref.GetString();
                    break;
                }
            return true;
        }
        catch (JsonException)
        {
            return false; // malformed draft → not counted
        }
    }

    private static bool TryGetPropertyCI(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var p in obj.EnumerateObject())
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        value = default;
        return false;
    }
}
