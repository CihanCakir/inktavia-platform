using System.Reflection;
using System.Text.Json;
using Aizen.Core.Domain;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;
using Aizen.Modules.Identity.Repository.Seed.MockData;
using Aizen.Modules.Payment.Abstraction;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Identity.Repository.Context.Seed.MockData;

/// <summary>
/// Idempotent mock data seeder for the Identity module.
/// Seeds demo users and profiles using stable IDs for local and development environments only.
/// </summary>
public sealed class IdentityMockDataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IdentityDbContext _db;
    private readonly IPasswordHasher<UserEntity> _hasher;
    private readonly MockDataSeedOptions _options;
    private readonly ILogger<IdentityMockDataSeeder> _logger;
    private readonly string _environment;

    public IdentityMockDataSeeder(
        IdentityDbContext db,
        IPasswordHasher<UserEntity> hasher,
        IOptions<MockDataSeedOptions> options,
        ILogger<IdentityMockDataSeeder> logger,
        string environment)
    {
        _db = db;
        _hasher = hasher;
        _options = options.Value;
        _logger = logger;
        _environment = environment;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled || !_options.RunOnStartup)
        {
            _logger.LogDebug("Identity MockData seeder is disabled.");
            return;
        }

        if (!_options.EnvironmentGuard.Contains(_environment, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Identity MockData seeder skipped: environment '{Environment}' is not in EnvironmentGuard {Guard}.",
                _environment, string.Join(", ", _options.EnvironmentGuard));
            return;
        }

        _logger.LogInformation("Identity MockData seeder starting (dataset: {DataSet}).", _options.DataSet);

        var basePath = Path.Combine(AppContext.BaseDirectory, "Seed", "Json", "MockData", _options.DataSet);

        // Maps seed JSON userId → actual DB userId for users that already existed with a different ID.
        var userIdRemap = new Dictionary<long, long>();

        await SeedUsersAsync(basePath, userIdRemap, ct);
        await SeedProfilesAsync(basePath, userIdRemap, ct);
        await AdvanceSequencesAsync(ct);

        _logger.LogInformation("Identity MockData seeder completed.");
    }

    private async Task SeedUsersAsync(string basePath, Dictionary<long, long> userIdRemap, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "identity-users.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Identity users seed file not found: {Path}", filePath);
            return;
        }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockUserSeedModel>>(stream, JsonOptions, ct)
            ?? [];

        foreach (var model in models)
        {
            var normalizedEmail = model.Email.ToUpperInvariant();

            // Check if user already exists by seed ID (idempotent re-run).
            if (await _db.Users.AnyAsync(u => u.Id == model.Id, ct))
                continue;

            // User may already exist with the same email but a different ID
            // (e.g. created by SeedIdentityBase). Record the remap so profile seeding
            // can reference the correct FK, then skip creation.
            var existingId = await _db.Users
                .Where(u => u.NormalizedEmail == normalizedEmail)
                .Select(u => (long?)u.Id)
                .FirstOrDefaultAsync(ct);

            if (existingId.HasValue)
            {
                userIdRemap[model.Id] = existingId.Value;
                _logger.LogDebug(
                    "User {Email} already exists with Id {ActualId} (seed Id {SeedId}), remapping.",
                    model.Email, existingId.Value, model.Id);
                continue;
            }

            // Use factory method since UserEntity() constructor is protected.
            // IPasswordHasher does not use the user instance for hashing, so a temporary
            // instance is safe here.
            var hashTarget = UserEntity.CreateLocal("_seed_hash_target", null, string.Empty, LoginType.Email);
            var passwordHash = _hasher.HashPassword(hashTarget, model.Password);

            var user = UserEntity.CreateLocal(
                email: model.Email,
                phone: null,
                passwordHash: passwordHash,
                loginType: (LoginType)model.LoginType);

            user.Id = model.Id;
            user.NormalizedUserName = normalizedEmail;
            user.NormalizedEmail = normalizedEmail;
            user.EmailConfirmed = true;
            user.ConcurrencyStamp = Guid.NewGuid().ToString();

            _db.Users.Add(user);

            try
            {
                await _db.SaveChangesAsync(ct);
                _logger.LogDebug("Seeded user {Id} ({Email}).", model.Id, model.Email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to seed user {Id} ({Email}), skipping.", model.Id, model.Email);
                _db.ChangeTracker.Clear();
            }
        }
    }

    private async Task SeedProfilesAsync(string basePath, Dictionary<long, long> userIdRemap, CancellationToken ct)
    {
        var filePath = Path.Combine(basePath, "identity-profiles.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Identity profiles seed file not found: {Path}", filePath);
            return;
        }

        await using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<MockProfileSeedModel>>(stream, JsonOptions, ct)
            ?? [];

        foreach (var model in models)
        {
            if (await _db.UserProfiles.AnyAsync(p => p.Id == model.Id, ct))
                continue;

            // Resolve the actual DB user ID — may differ from seed if user was pre-created.
            var actualUserId = userIdRemap.TryGetValue(model.UserId, out var remapped)
                ? remapped
                : model.UserId;

            if (!await _db.Users.AnyAsync(u => u.Id == actualUserId, ct))
            {
                _logger.LogWarning(
                    "Skipping profile {ProfileId}: user {UserId} does not exist in the database.",
                    model.Id, actualUserId);
                continue;
            }

            var profile = UserProfileEntity.Create(
                userId: actualUserId,
                firstName: model.FirstName,
                lastName: model.LastName,
                taxpayerType: (TaxpayerType)model.TaxpayerType
            );

            profile.RoleContext = (WorkshopRoleContext)model.RoleContext;
            profile.Id = model.Id;
            profile.CreateDate = DateTime.UtcNow;
            profile.ModifyDate = DateTime.UtcNow;
            profile.IsActive = model.ProfileStatus == 2;
            profile.IsDeleted = false;

            // Apply approval state via domain methods
            if (model.ApprovalStatus == 1) // Approved
                ApplyApproved(profile);
            else if (model.ApprovalStatus == 2 && !string.IsNullOrWhiteSpace(model.RejectReason)) // Rejected
                ApplyRejected(profile, model.RejectReason!);

            // Force ProfileStatus via reflection (private setter, seed-only)
            SetPrivateProperty(profile, "Status", (ProfileStatus)model.ProfileStatus);

            _db.UserProfiles.Add(profile);

            try
            {
                await _db.SaveChangesAsync(ct);

                // Set as active profile on user when status is Active and approved
                if (model.ProfileStatus == 2 && model.ApprovalStatus == 1)
                {
                    await SetActiveProfileAsync(actualUserId, model.Id, ct);
                }

                _logger.LogDebug("Seeded profile {Id} for user {UserId}.", model.Id, model.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to seed profile {Id} for user {UserId}, skipping.", model.Id, model.UserId);
                _db.ChangeTracker.Clear();
            }
        }
    }

    private async Task SetActiveProfileAsync(long userId, long profileId, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return;

        // Only set if no active profile is assigned yet
        if (user.ActiveProfileId.HasValue) return;

        SetPrivateProperty(user, "ActiveProfileId", (long?)profileId);
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    private static void ApplyApproved(UserProfileEntity profile)
    {
        SetPrivateProperty(profile, "ApprovalStatus", ApprovalStatus.Approved);
        SetPrivateProperty(profile, "ApprovedAt", (DateTime?)DateTime.UtcNow);
    }

    private static void ApplyRejected(UserProfileEntity profile, string reason)
    {
        SetPrivateProperty(profile, "ApprovalStatus", ApprovalStatus.Rejected);
        SetPrivateProperty(profile, "RejectedAt", (DateTime?)DateTime.UtcNow);
        SetPrivateProperty(profile, "RejectReason", (string?)reason);
    }

    private static void SetPrivateProperty<T>(object obj, string propertyName, T value)
    {
        var prop = obj.GetType().GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop is null) return;

        // For properties with a private/protected setter, invoke the set method directly
        // instead of prop.SetValue(), which throws when the accessor is non-public.
        var setter = prop.GetSetMethod(nonPublic: true);
        setter?.Invoke(obj, [value]);
    }

    private async Task AdvanceSequencesAsync(CancellationToken ct)
    {
        try
        {
            // Tablolar 'identity' şemasına taşındığı için ham SQL şema-nitelikli olmalı: bu sorgu search_path'e
            // (public) güveniyordu; niteliksiz kalırsa taşımadan sonra tabloları bulamaz.
            await _db.Database.ExecuteSqlRawAsync(@"
                DO $$
                DECLARE seq_name text;
                BEGIN
                    SELECT pg_get_serial_sequence('identity.""Users""', 'Id') INTO seq_name;
                    IF seq_name IS NOT NULL THEN
                        PERFORM setval(seq_name, GREATEST(100000, COALESCE((SELECT MAX(""Id"") FROM identity.""Users""), 0)));
                    END IF;

                    SELECT pg_get_serial_sequence('identity.""UserProfiles""', 'Id') INTO seq_name;
                    IF seq_name IS NOT NULL THEN
                        PERFORM setval(seq_name, GREATEST(100000, COALESCE((SELECT MAX(""Id"") FROM identity.""UserProfiles""), 0)));
                    END IF;
                END $$;", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not advance Identity sequences (non-fatal).");
        }
    }

    // --- Seed models ---

    private sealed record MockUserSeedModel(
        long Id,
        string Email,
        string Password,
        string FirstName,
        string LastName,
        int RoleContext,
        int LoginType,
        int ApprovalStatus);

    private sealed record MockProfileSeedModel(
        long Id,
        long UserId,
        string FirstName,
        string LastName,
        int RoleContext,
        int TaxpayerType,
        int ApprovalStatus,
        int ProfileStatus,
        string? RejectReason = null);
}
