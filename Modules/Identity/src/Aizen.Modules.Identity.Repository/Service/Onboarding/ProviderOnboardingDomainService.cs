using System.Text.Json;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Entities.Onboarding;
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
    private readonly IdentityDbContext _db;
    private readonly ILogger<ProviderOnboardingDomainService> _logger;

    private const int CurrentSchemaVersion = 1;

    public ProviderOnboardingDomainService(
        IProviderOnboardingRepository repo,
        IUserProfileRepository profileRepo,
        IdentityDbContext db,
        ILogger<ProviderOnboardingDomainService> logger)
    {
        _repo = repo;
        _profileRepo = profileRepo;
        _db = db;
        _logger = logger;
    }

    public Task<ProviderOnboardingEntity?> GetAsync(long profileId, CancellationToken ct)
        => GetOrCreateAsync(profileId, ct);

    public async Task SaveStepAsync(long profileId, string step, string stepStatus, string stepDataJson, int schemaVersion, CancellationToken ct)
    {
        if (schemaVersion != CurrentSchemaVersion)
            throw new AizenBusinessException($"Unsupported schema version {schemaVersion}. Expected {CurrentSchemaVersion}.");

        var entity = await GetOrCreateAsync(profileId, ct)
            ?? throw new AizenBusinessException("Provider profile not found.");

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
            ?? throw new AizenBusinessException("Onboarding record not found.");

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
            throw new AizenBusinessException("Submission incomplete: " + string.Join("; ", missing));

        // ── Submit (checks step statuses; idempotent when already Submitted) ──
        entity.Submit(DateTime.UtcNow);
        _repo.Update(entity);

        // Mirror authoritative fields onto UserProfileEntity — failure must fail the submit.
        var profile = await _profileRepo.GetProfileByIdAsync(profileId);
        if (profile is not null)
        {
            MirrorDraftToProfile(draft, profile);
            _profileRepo.UpdateProfileAsync(profile);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task RequestRevisionAsync(long profileId, string[] steps, string note, CancellationToken ct)
    {
        var entity = await _repo.GetByProfileIdAsync(profileId, ct)
            ?? throw new AizenBusinessException("Onboarding record not found.");

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
            var city = bi.TryGetProperty("city", out var c) ? c.GetString() : null;
            var country = bi.TryGetProperty("country", out var co) ? co.GetString() : null;
            var firstName = bi.TryGetProperty("ownerFirstName", out var fn) ? fn.GetString() : null;
            var lastName = bi.TryGetProperty("ownerLastName", out var ln) ? ln.GetString() : null;
            var bio = bi.TryGetProperty("bio", out var b) ? b.GetString() : null;

            if (!string.IsNullOrWhiteSpace(companyName))
                profile.SetCompanyName(companyName);
            if (!string.IsNullOrWhiteSpace(city) || !string.IsNullOrWhiteSpace(country))
                profile.SetLocation(city, country);
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                profile.ChangeName(firstName, lastName);
            if (bio is not null)
                profile.UpdateBio(bio);
        }
    }
}
