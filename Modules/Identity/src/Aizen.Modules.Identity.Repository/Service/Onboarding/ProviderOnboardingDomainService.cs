using System.Text.Json;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Entities.Onboarding;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Repository.Context;
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
        => _repo.GetByProfileIdAsync(profileId, ct);

    public async Task SaveStepAsync(long profileId, string step, string stepStatus, string stepDataJson, int schemaVersion, CancellationToken ct)
    {
        if (schemaVersion != CurrentSchemaVersion)
            throw new AizenBusinessException($"Unsupported schema version {schemaVersion}. Expected {CurrentSchemaVersion}.");

        var entity = await _repo.GetByProfileIdAsync(profileId, ct)
            ?? throw new AizenBusinessException("Onboarding record not found.");

        entity.SaveStep(step, stepStatus, stepDataJson, DateTime.UtcNow);
        _repo.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SubmitAsync(long profileId, CancellationToken ct)
    {
        var entity = await _repo.GetByProfileIdAsync(profileId, ct)
            ?? throw new AizenBusinessException("Onboarding record not found.");

        entity.Submit(DateTime.UtcNow);
        _repo.Update(entity);

        // Mirror authoritative fields onto UserProfileEntity
        try
        {
            var profile = await _profileRepo.GetProfileByIdAsync(profileId);
            if (profile is not null)
            {
                var draft = entity.GetDraft();
                MirrorDraftToProfile(draft, profile);
                _profileRepo.UpdateProfileAsync(profile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to mirror onboarding draft to profile {ProfileId}.", profileId);
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
