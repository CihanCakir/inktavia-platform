using System.Diagnostics;
using System.Text.Json;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Common.Abstraction.ViewModel; // FAZ12B #65: AizenErrorCode (kararlı onboarding hata kodları)
using Aizen.Modules.Identity.Abstraction.RemoteCall;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Entities.Onboarding;
using Aizen.Modules.Identity.Domain.Entities.ProviderServiceCategory;
using Aizen.Modules.Identity.Domain.Enum;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Identity.Repository.Identity.Service.Onboarding;

public sealed class ProviderOnboardingDomainService : IProviderOnboardingDomainService
{
    private readonly IProviderOnboardingRepository _repo;
    private readonly IUserProfileRepository _profileRepo;
    private readonly IProviderServiceCategoryRepository _categories;
    private readonly IdentityDbContext _db;
    private readonly IIdentityReferenceDataRemoteCall _referenceData;
    private readonly ILogger<ProviderOnboardingDomainService> _logger;

    private const int CurrentSchemaVersion = 1;

    public ProviderOnboardingDomainService(
        IProviderOnboardingRepository repo,
        IUserProfileRepository profileRepo,
        IProviderServiceCategoryRepository categories,
        IdentityDbContext db,
        IIdentityReferenceDataRemoteCall referenceData,
        ILogger<ProviderOnboardingDomainService> logger)
    {
        _repo = repo;
        _profileRepo = profileRepo;
        _categories = categories;
        _db = db;
        _referenceData = referenceData;
        _logger = logger;
    }

    public Task<ProviderOnboardingEntity?> GetAsync(long profileId, CancellationToken ct)
        => GetOrCreateAsync(profileId, ct);

    public async Task SaveStepAsync(long profileId, string step, string stepStatus, string stepDataJson, int schemaVersion, CancellationToken ct)
    {
        if (schemaVersion != CurrentSchemaVersion)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingSchemaVersionUnsupported, $"Unsupported schema version {schemaVersion}. Expected {CurrentSchemaVersion}.");

        // Validate city code against ReferenceData when saving the OperatingRegion step.
        if (step == "OperatingRegion")
            await ValidateCityCodeInJsonAsync(stepDataJson, ct);

        var entity = await GetOrCreateAsync(profileId, ct)
            ?? throw new AizenBusinessException((int)AizenErrorCode.ProviderProfileNotFound, "Provider profile not found.");

        entity.SaveStep(step, stepStatus, stepDataJson, DateTime.UtcNow);
        _repo.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// The onboarding record is a lazily-initialised part of the provider aggregate: a profile that has one
    /// without the other cannot do anything at all.
    ///
    /// Only the legacy <c>RegisterOrganizer</c> command ever created this row. Every provider who signed up
    /// through the SPA is provisioned from Keycloak instead, so they were born with a profile and **no onboarding
    /// record** — `GET /onboarding` returned nothing and every step save died with "Onboarding record not found".
    /// The provisioning path now creates it, but that only helps new profiles; creating it on first read heals the
    /// ones that already exist, and makes the invariant impossible to break again from a path we have not thought of.
    ///
    /// Returns null only when the *profile* does not exist — that is a real error, not something to paper over.
    /// </summary>
    private async Task<ProviderOnboardingEntity?> GetOrCreateAsync(long profileId, CancellationToken ct)
    {
        var existing = await _repo.GetByProfileIdAsync(profileId, ct);
        if (existing is not null) return existing;

        var profile = await _profileRepo.GetProfileByIdAsync(profileId);
        if (profile is null)
        {
            _logger.LogWarning("Onboarding requested for profile {ProfileId}, which does not exist.", profileId);
            return null;
        }

        var entity = ProviderOnboardingEntity.Create(profileId, profile.UserId);
        await _repo.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created the missing onboarding record for profile {ProfileId}.", profileId);
        return entity;
    }

    public async Task SubmitAsync(long profileId, CancellationToken ct)
    {
        var entity = await _repo.GetByProfileIdAsync(profileId, ct)
            ?? throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingRecordNotFound, "Onboarding record not found.");

        // Double-submit is idempotent — skip validation and mirroring if already submitted.
        if (entity.Status == ProviderOnboardingStatus.Submitted)
            return;

        // ── Completeness validation ──────────────────────────────────────
        var draft = entity.GetDraft();
        var requiredSteps = new[] { "BusinessIdentity", "ServiceCapabilities", "OperatingRegion", "ComplianceVerification" };
        var missing = new List<string>();

        foreach (var step in requiredSteps)
        {
            if (!draft.ContainsKey(step) || draft[step].ValueKind == JsonValueKind.Null ||
                (draft[step].ValueKind == JsonValueKind.Object && !draft[step].EnumerateObject().Any()))
            {
                missing.Add($"Step '{step}' has no saved data.");
            }
        }

        // Validate the operating city against ReferenceData. An unknown code is rejected — not stored,
        // not uppercased into something plausible. This is the gate that prevents the vocabularies from
        // drifting apart again.
        if (draft.TryGetValue("OperatingRegion", out var orStep))
        {
            var cityCode = orStep.TryGetProperty("cityCode", out var cc) ? cc.GetString() : null;
            var countryCode = orStep.TryGetProperty("country", out var co) ? co.GetString() : null;
            if (string.IsNullOrWhiteSpace(cityCode))
                missing.Add("OperatingRegion: city code is required.");
            else if (!await IsCityCodeValidAsync(countryCode ?? "TR", cityCode, ct))
                missing.Add($"OperatingRegion: city code '{cityCode}' is not a recognised ReferenceData city.");
        }

        var documents = await _db.VerificationDocuments
            .Where(d => d.ProfileId == profileId && !d.IsDeleted)
            .ToListAsync(ct);

        if (documents.Count == 0)
            missing.Add("At least one verification document is required.");

        var rejectedDocs = documents
            .Where(d => d.ReviewStatus == DocumentReviewStatus.Rejected)
            .ToList();
        foreach (var doc in rejectedDocs)
            missing.Add($"Document '{doc.Name}' (type: {doc.DocumentType}) has been rejected and must be replaced.");

        if (missing.Count > 0)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingSubmissionIncomplete, "Submission incomplete: " + string.Join("; ", missing));

        // ── Submit (checks step statuses; idempotent when already Submitted) ──
        entity.Submit(DateTime.UtcNow);
        _repo.Update(entity);

        // Mirror authoritative fields onto UserProfileEntity — failure must fail the submit.
        var profile = await _profileRepo.GetProfileByIdAsync(profileId);
        if (profile is not null)
        {
            MirrorDraftToProfile(draft, profile);
            _profileRepo.UpdateProfileAsync(profile);

            // I2 — normalize the provider's declared service categories (ServiceCapabilities.selectedServiceCategoryIds)
            // into the queryable provider_service_categories table so GetProvidersForArea can match by category.
            var categoryCodes = ExtractServiceCategoryCodes(draft);
            await _categories.ReplaceForProfileAsync(profileId, profile.UserId, categoryCodes, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task RequestRevisionAsync(long profileId, string[] steps, string note, CancellationToken ct)
    {
        var entity = await _repo.GetByProfileIdAsync(profileId, ct)
            ?? throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingRecordNotFound, "Onboarding record not found.");

        entity.RequestRevision(steps, note, DateTime.UtcNow);
        _repo.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task EnsureOnboardingRowAsync(long profileId, long userId, CancellationToken ct)
    {
        var existing = await _repo.GetByProfileIdAsync(profileId, ct);
        if (existing is not null) return;

        var entity = ProviderOnboardingEntity.Create(profileId, userId);
        await _repo.AddAsync(entity, ct);
    }

    private static void MirrorDraftToProfile(Dictionary<string, JsonElement> draft, UserProfileEntity profile)
    {
        if (draft.TryGetValue("BusinessIdentity", out var bi))
        {
            var companyName = bi.TryGetProperty("companyName", out var cn) ? cn.GetString() : null;
            var firstName = bi.TryGetProperty("ownerFirstName", out var fn) ? fn.GetString() : null;
            var lastName = bi.TryGetProperty("ownerLastName", out var ln) ? ln.GetString() : null;
            var bio = bi.TryGetProperty("bio", out var b) ? b.GetString() : null;

            if (!string.IsNullOrWhiteSpace(companyName))
                profile.SetCompanyName(companyName);
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                profile.ChangeName(firstName, lastName);
            if (bio is not null)
                profile.UpdateBio(bio);
        }

        // The provider's operating city is a ReferenceData city code (e.g. "35" for Izmir, "48" for Mugla).
        // The field name is "cityCode" — the legacy "cityOrPort" accepted free text (district names, marina
        // names, anything) and that is exactly the class of bug we are removing. No fallback to it.
        //
        // Bodrum, Çeşme etc. are districts of a province (Muğla, İzmir). The canonical code is the province
        // plate code — port/district refinement belongs to GeoDiscovery, not here.
        if (draft.TryGetValue("OperatingRegion", out var or))
        {
            var cityCode = or.TryGetProperty("cityCode", out var cc) ? cc.GetString() : null;
            var countryCode = or.TryGetProperty("country", out var co) ? co.GetString() : null;
            if (!string.IsNullOrWhiteSpace(cityCode))
                profile.SetLocation(cityCode.Trim().ToUpperInvariant(), countryCode?.Trim().ToUpperInvariant() ?? profile.Country);

            // Phase-3 — materialize the optional business coordinates for distance pricing. IDEMPOTENT: only when the
            // provider hasn't already set a business location (a manual profile edit wins; a re-submit won't clobber it).
            if (profile.BusinessLatitude is null && profile.BusinessLongitude is null
                && OnboardingCoordinateRules.TryReadBusinessCoords(or, out var blat, out var blng, out var blabel))
            {
                profile.SetBusinessLocation(blat, blng, blabel);
            }
        }
    }

    /// <summary>
    /// Extracts the provider's declared service category codes from the onboarding draft
    /// (ServiceCapabilities.selectedServiceCategoryIds, a string array of onboarding category ids, e.g. "hull-paint").
    /// These are a fixed onboarding vocabulary (no ReferenceData lookup group exists for them yet); stored normalized.
    /// </summary>
    private static IReadOnlyList<string> ExtractServiceCategoryCodes(Dictionary<string, JsonElement> draft)
    {
        if (!draft.TryGetValue("ServiceCapabilities", out var sc) || sc.ValueKind != JsonValueKind.Object)
            return Array.Empty<string>();
        if (!sc.TryGetProperty("selectedServiceCategoryIds", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        var codes = new List<string>();
        foreach (var el in arr.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.String) continue;
            var v = el.GetString();
            // CANON — store the canonical lower(SERVICE_PROVIDER_CATEGORY.Code) form. Accepts both the legacy
            // onboarding ids and the post-CANON-e canonical codes during the frontend transition.
            if (!string.IsNullOrWhiteSpace(v)) codes.Add(ProviderServiceCategoryCanonicalMap.ToCanonical(v));
        }
        return codes;
    }

    /// <summary>
    /// Validates a city code against ReferenceData. Returns true only if the code is a known, active city.
    /// </summary>
    private async Task<bool> IsCityCodeValidAsync(string countryCode, string cityCode, CancellationToken ct)
    {
        try
        {
            var result = await _referenceData.GetCity(
                countryCode.Trim().ToUpperInvariant(),
                cityCode.Trim().ToUpperInvariant());

            // Arama BAŞARILI. Şehir yoksa (Body null — endpoint bulunamayanda 200 + null döner) veya pasifse
            // bu GERÇEK bir iş hatasıdır (kullanıcının verisi geçersiz) → false. Bunu çağıran AizenBusinessException'a çevirir.
            return result.Body is not null && result.Body.IsActive;
        }
        catch (Exception ex)
        {
            // Aramanın KENDİSİ başarısız oldu (BaseAddress yok, 403, timeout, 5xx, ağ hatası...). Bu bir DOĞRULAMA
            // hatası DEĞİL, altyapı/erişilebilirlik hatasıdır. Eskiden burada 'return false' vardı ve bu, "şehriniz
            // tanınmıyor" iş hatasıyla aynı cümleye çöküp asıl defekti (eksik BaseUrl) gizliyordu. Artık yukarı-akış
            // hatası olarak yüzeye çıkar (Core.Api middleware → 502), asla geçersiz-veri gibi görünmez.
            var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
            _logger.LogError(ex,
                "[{Code}] ReferenceData city lookup failed for {CountryCode}/{CityCode}. correlationId={CorrelationId}",
                AizenUpstreamException.StableCode, countryCode, cityCode, correlationId);
            throw new AizenUpstreamException(
                correlationId,
                publicMessage: "Şehir doğrulaması şu an yapılamıyor. Lütfen daha sonra tekrar deneyin.");
        }
    }

    /// <summary>
    /// Validates the cityCode field in a raw OperatingRegion JSON string. Called on save-step.
    /// </summary>
    private async Task ValidateCityCodeInJsonAsync(string stepDataJson, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(stepDataJson);
            var root = doc.RootElement;
            var cityCode = root.TryGetProperty("cityCode", out var cc) ? cc.GetString() : null;
            var countryCode = root.TryGetProperty("country", out var co) ? co.GetString() : null;
            if (!string.IsNullOrWhiteSpace(cityCode) && !await IsCityCodeValidAsync(countryCode ?? "TR", cityCode, ct))
                throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingCityNotRecognised, $"City code '{cityCode}' is not a recognised ReferenceData city.");

            // Phase-3 — optional business coordinates. Same enforcement shape as cityCode: validated WHEN PRESENT
            // at save (not blocking step-Completed), never required (materialized on submit; distance pricing is
            // null-safe without them). Both-or-neither lat/lng + a plausible Turkey bbox.
            OnboardingCoordinateRules.Validate(root);
        }
        catch (AizenBusinessException) { throw; }
        catch (JsonException) { /* Malformed JSON — let the step save handle it */ }
    }
}
