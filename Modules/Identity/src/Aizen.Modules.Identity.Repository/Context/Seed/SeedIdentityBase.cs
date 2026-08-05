using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Entities.UserAgreement;
using Aizen.Modules.Payment.Abstraction;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Identity.Repository.Context.Seed
{

    public static class SeedIdentityBase
    {
        public static async Task RunAsync(IServiceProvider sp, CancellationToken ct = default)
        {
            using var scope = sp.CreateScope();
            var services = scope.ServiceProvider;

            var db = services.GetRequiredService<IdentityDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<RoleEntity>>();
            var userManager = services.GetRequiredService<UserManager<UserEntity>>();
            var cfg = services.GetRequiredService<IConfiguration>();

            // db.Database.GetMigrations()
            // 0) migrate
            await db.Database.MigrateAsync(ct);

            // 1) ROLE TYPES (FK için şart)
            var roleTypeAdminId = await EnsureRoleTypeAsync(db, code: "Admin", description: "System administrator", ct);
            var roleTypeConsumerId = await EnsureRoleTypeAsync(db, code: "Consumer", description: "Participant user", ct);
            var roleTypeOrganizerId = await EnsureRoleTypeAsync(db, code: "Organizer", description: "Event organizer", ct);
            var roleTypeVenueId = await EnsureRoleTypeAsync(db, code: "Venue", description: "Venue manager", ct);

            // 2) ROLES (RoleTypeId zorunlu)
            await EnsureRoleAsync(roleManager, "Admin", description: "Platform admin", roleTypeId: roleTypeAdminId);
            await EnsureRoleAsync(roleManager, "Consumer", description: "Participant user", roleTypeId: roleTypeConsumerId);
            await EnsureRoleAsync(roleManager, "Organizer", description: "Event organizer", roleTypeId: roleTypeOrganizerId);
            await EnsureRoleAsync(roleManager, "Venue", description: "Venue manager", roleTypeId: roleTypeVenueId);

            // 3) AGREEMENTS (idempotent upsert)
            await EnsureAgreementAsync(db,
                name: "Kişisel Verilerin Korunması",
                agreementType: "KVKK",
                version: 1.0m,
                isOptional: false,
                ct: ct);

            await EnsureAgreementAsync(db,
                name: "Kullanım Koşulları",
                agreementType: "TermsOfService",
                version: 1.0m,
                isOptional: false,
                ct: ct);

            // 4) ADMIN USER (EmailConfirmed + Participant profil + Admin rol)
            var adminEmail = cfg["Seed:Admin:Email"] ?? "admin@inktavia.local";
            var adminPass = cfg["Seed:Admin:Password"] ?? "Admin!123";
            var adminFirst = cfg["Seed:Admin:FirstName"] ?? "System";
            var adminLast = cfg["Seed:Admin:LastName"] ?? "Admin";

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                // local hesap (Identity parola hash'ler)
                admin = UserEntity.CreateLocal(
                    email: adminEmail,
                    phone: null,
                    passwordHash: "", // UserManager.CreateAsync set eder
                    loginType: LoginType.Email
                );
                admin.EmailConfirmed = true;

                var createRes = await userManager.CreateAsync(admin, adminPass);
                if (!createRes.Succeeded)
                    throw new Exception("Admin user creation failed: " + string.Join(", ", createRes.Errors.Select(e => e.Description)));

                // refresh (Id & tracking güvence)
                admin = await userManager.FindByEmailAsync(adminEmail);
            }
            else if (!admin.EmailConfirmed)
            {
                admin.EmailConfirmed = true;
                await userManager.UpdateAsync(admin);
            }

            // Admin → Participant profil (yoksa oluştur + aktifle)
            var hasParticipant = await db.UserProfiles
                .AnyAsync(p => p.UserId == admin!.Id && p.RoleContext == WorkshopRoleContext.Participant, ct);

            if (!hasParticipant)
            {
                var profile = UserProfileEntity.Create(
                    userId: admin.Id,
                    firstName: adminFirst,
                    lastName: adminLast,
                    taxpayerType: TaxpayerType.Individual
                );
                profile.RoleContext = WorkshopRoleContext.Participant;

                db.UserProfiles.Add(profile);
                await db.SaveChangesAsync(ct);

                admin.AddProfile(profile, markAsActive: true);
                db.Update(admin);
                await db.SaveChangesAsync(ct);
            }

            // Admin rol ataması (custom UserRoleEntity nedeniyle DbContext üzerinden)
            await EnsureUserInRoleAsync(db, admin, "Admin", ct);

            // OTP-ready admin: ensure KeycloakSubjectId + Admin profile (Dev/Local only)
            var env = cfg["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (env is "Local" or "Development")
            {
                // Set a KeycloakSubjectId if missing (dev placeholder or from config)
                if (string.IsNullOrWhiteSpace(admin.KeycloakSubjectId))
                {
                    var configuredSub = cfg["Seed:Admin:KeycloakSubjectId"];
                    if (string.IsNullOrWhiteSpace(configuredSub))
                    {
                        // Stable dev placeholder — Kickoff 2 must replace with the real Keycloak subject id
                        configuredSub = "00000000-0000-0000-0000-admin0000001";
                        // Log warning at console since ILogger is not available in static seed
                        Console.WriteLine(
                            "[WARN] SeedIdentityBase: Admin user assigned dev-placeholder KeycloakSubjectId. " +
                            "Kickoff 2 must replace it with the real Keycloak subject id of the admin user.");
                    }

                    admin.SetKeycloakSubjectId(configuredSub);
                    await userManager.UpdateAsync(admin);
                }

                // Ensure Admin profile exists (WorkshopRoleContext.Admin)
                var hasAdminProfile = await db.UserProfiles
                    .AnyAsync(p => p.UserId == admin.Id && p.RoleContext == WorkshopRoleContext.Admin, ct);

                if (!hasAdminProfile)
                {
                    var adminProfile = UserProfileEntity.Create(
                        userId: admin.Id,
                        firstName: adminFirst,
                        lastName: adminLast,
                        taxpayerType: TaxpayerType.Individual
                    );
                    adminProfile.RoleContext = WorkshopRoleContext.Admin;

                    db.UserProfiles.Add(adminProfile);
                    await db.SaveChangesAsync(ct);

                    admin.AddProfile(adminProfile, markAsActive: false);
                    db.Update(admin);
                    await db.SaveChangesAsync(ct);
                }

                // If admin email is the OTP-ready one, make sure it's set
                var otpAdminEmail = cfg["Seed:Admin:Email"] ?? "admin@inktavia.local";
                if (!string.Equals(otpAdminEmail, "admin@inktavia.local", StringComparison.OrdinalIgnoreCase)
                    && admin.Email != null
                    && string.Equals(admin.Email, otpAdminEmail, StringComparison.OrdinalIgnoreCase))
                {
                    // Email already matches, nothing to do
                }

                // ---- OTP-ready PARTICIPANT (mobile) — Dev/Local only ----------------------------------
                // M2a verification enabler: ensure one participant is OTP-login-ready with a real (or
                // dev-placeholder) KeycloakSubjectId linked in Identity + an active Participant profile.
                // Linking the REAL Keycloak subject is M2d provisioning; this is a minimal DEV-only seed
                // link so the Identity-only request→verify→consume log test can run. The email defaults to
                // mobile.user@inktavia.com, the Keycloak seed user that already holds the mobile_user role.
                var participantEmail = cfg["Seed:Participant:Email"] ?? "mobile.user@inktavia.com";
                var participantPass = cfg["Seed:Participant:Password"] ?? "Password123!";
                var participantFirst = cfg["Seed:Participant:FirstName"] ?? "Mobile";
                var participantLast = cfg["Seed:Participant:LastName"] ?? "User";

                var participant = await userManager.FindByEmailAsync(participantEmail);
                if (participant is null)
                {
                    participant = UserEntity.CreateLocal(
                        email: participantEmail,
                        phone: null,
                        passwordHash: "",
                        loginType: LoginType.Email
                    );
                    participant.EmailConfirmed = true;

                    var createRes = await userManager.CreateAsync(participant, participantPass);
                    if (!createRes.Succeeded)
                        throw new Exception("Participant user creation failed: " + string.Join(", ", createRes.Errors.Select(e => e.Description)));

                    participant = await userManager.FindByEmailAsync(participantEmail);
                }
                else if (!participant.EmailConfirmed)
                {
                    participant.EmailConfirmed = true;
                    await userManager.UpdateAsync(participant);
                }

                // Consumer role (participant/mobile user)
                await EnsureUserInRoleAsync(db, participant!, "Consumer", ct);

                // Active Participant profile (the OTP-login gate)
                var hasParticipantProfile = await db.UserProfiles
                    .AnyAsync(p => p.UserId == participant!.Id && p.RoleContext == WorkshopRoleContext.Participant, ct);

                if (!hasParticipantProfile)
                {
                    var participantProfile = UserProfileEntity.Create(
                        userId: participant!.Id,
                        firstName: participantFirst,
                        lastName: participantLast,
                        taxpayerType: TaxpayerType.Individual
                    );
                    participantProfile.RoleContext = WorkshopRoleContext.Participant;

                    db.UserProfiles.Add(participantProfile);
                    await db.SaveChangesAsync(ct);

                    participant!.AddProfile(participantProfile, markAsActive: true);
                    db.Update(participant);
                    await db.SaveChangesAsync(ct);
                }

                // KeycloakSubjectId (M2c): resolve the REAL Keycloak subject of the mobile user so the OTP
                // SPI can resolve the user at handoff (the M2a placeholder can't be resolved — this is what
                // blocked M2b's final step). Best-effort admin lookup; falls back to config/placeholder.
                var realSubject = await ResolveKeycloakSubjectByEmailAsync(cfg, participantEmail, ct);
                if (!string.IsNullOrWhiteSpace(realSubject))
                {
                    if (!string.Equals(participant!.KeycloakSubjectId, realSubject, StringComparison.Ordinal))
                    {
                        participant.SetKeycloakSubjectId(realSubject!);
                        await userManager.UpdateAsync(participant);
                        var masked = realSubject!.Length >= 8 ? realSubject[..8] + "…" : "…";
                        Console.WriteLine($"[SEED] Participant linked to real Keycloak subject ({masked}) for {participantEmail}.");
                    }
                }
                else if (string.IsNullOrWhiteSpace(participant!.KeycloakSubjectId))
                {
                    // Lookup unavailable and nothing set yet — config value, else the stable dev placeholder.
                    var configuredSub = cfg["Seed:Participant:KeycloakSubjectId"];
                    if (string.IsNullOrWhiteSpace(configuredSub))
                    {
                        configuredSub = "00000000-0000-0000-0000-participant1";
                        Console.WriteLine(
                            "[WARN] SeedIdentityBase: could not resolve the real Keycloak subject for the mobile user; " +
                            "assigned dev-placeholder KeycloakSubjectId (OTP handoff will fail user-resolution until linked).");
                    }

                    participant.SetKeycloakSubjectId(configuredSub);
                    await userManager.UpdateAsync(participant);
                }
            }
        }

        // Best-effort DEV lookup of a Keycloak user's subject id by email via the Admin API, using the
        // Identity module's configured Keycloak admin service account (IdentityKeycloak:*). Returns null on
        // any failure so seeding never breaks. No secrets/tokens are logged.
        private static async Task<string?> ResolveKeycloakSubjectByEmailAsync(
            IConfiguration cfg, string email, CancellationToken ct)
        {
            try
            {
                var baseUrl = (cfg["IdentityKeycloak:BaseUrl"] ?? "http://keycloak:8080").TrimEnd('/');
                var realm = cfg["IdentityKeycloak:Realm"] ?? "inktavia-realm";
                var clientId = cfg["IdentityKeycloak:AdminClientId"];
                var clientSecret = cfg["IdentityKeycloak:AdminClientSecret"];
                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                    return null;

                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };

                using var tokenContent = new System.Net.Http.FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = clientId!,
                    ["client_secret"] = clientSecret!,
                });
                using var tokenResp = await http.PostAsync(
                    $"{baseUrl}/realms/{realm}/protocol/openid-connect/token", tokenContent, ct);
                if (!tokenResp.IsSuccessStatusCode) return null;

                using var tokenDoc = System.Text.Json.JsonDocument.Parse(await tokenResp.Content.ReadAsStringAsync(ct));
                if (!tokenDoc.RootElement.TryGetProperty("access_token", out var atEl)) return null;
                var accessToken = atEl.GetString();
                if (string.IsNullOrEmpty(accessToken)) return null;

                using var usersReq = new System.Net.Http.HttpRequestMessage(
                    System.Net.Http.HttpMethod.Get,
                    $"{baseUrl}/admin/realms/{realm}/users?email={Uri.EscapeDataString(email)}&exact=true");
                usersReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                using var usersResp = await http.SendAsync(usersReq, ct);
                if (!usersResp.IsSuccessStatusCode) return null;

                using var usersDoc = System.Text.Json.JsonDocument.Parse(await usersResp.Content.ReadAsStringAsync(ct));
                if (usersDoc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) return null;
                foreach (var u in usersDoc.RootElement.EnumerateArray())
                    if (u.TryGetProperty("id", out var idEl))
                        return idEl.GetString();

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ---------- helpers ----------

        // RoleTypeEntity upsert → Id döner
        private static async Task<long> EnsureRoleTypeAsync(
            IdentityDbContext db,
            string code,
            string? description,
            CancellationToken ct)
        {
            // Not: Entity alanları senin modeline göre Name/Code olabilir.
            var rt = await db.Set<RoleTypeEntity>()
                .FirstOrDefaultAsync(t => t.Name == code && !t.IsDeleted, ct);

            if (rt is null)
            {
                rt = new RoleTypeEntity
                {
                    Name = code,
                    Description = description,
                    CreateDate = DateTime.UtcNow,
                    ModifyDate = DateTime.UtcNow,
                    IsDeleted = false
                };
                db.Set<RoleTypeEntity>().Add(rt);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                var dirty = false;
                if (rt.Description != description) { rt.Description = description; dirty = true; }
                if (dirty)
                {
                    rt.ModifyDate = DateTime.UtcNow;
                    db.Update(rt);
                    await db.SaveChangesAsync(ct);
                }
            }

            return rt.Id;
        }

        private static async Task EnsureRoleAsync(
            RoleManager<RoleEntity> roleManager,
            string roleName,
            string? description,
            long roleTypeId)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new RoleEntity
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    Description = description,
                    RoleTypeId = roleTypeId,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                var res = await roleManager.CreateAsync(role);
                if (!res.Succeeded)
                    throw new Exception($"Role seed failed ({roleName}): " + string.Join(", ", res.Errors.Select(e => e.Description)));
            }
            else
            {
                var dirty = false;
                if (role.Description != description) { role.Description = description; dirty = true; }
                if (role.RoleTypeId != roleTypeId) { role.RoleTypeId = roleTypeId; dirty = true; }
                if (dirty)
                {
                    role.ModifiedAt = DateTime.UtcNow;
                    await roleManager.UpdateAsync(role);
                }
            }
        }

private static async Task EnsureAgreementAsync(
    IdentityDbContext db,
    string name,
    string agreementType,
    decimal version,
    bool isOptional,
    CancellationToken ct)
{
    // tracking açık kalabilir; proxy artık oluşturulabilir
    var ag = await db.Agreements.FirstOrDefaultAsync(a => a.AgreementType == agreementType, ct);
    if (ag is null)
    {
        ag = new AgreementEntity(
            name: name,
            initialVersion: version,
            agreementType: agreementType,
            isOptional: isOptional
        );
        db.Agreements.Add(ag);
        await db.SaveChangesAsync(ct);
        return;
    }

    // reflection yok — domain metodlarını kullan
    ag.UpdateMeta(name, isOptional);
    ag.BumpVersionIfHigher(version);

    db.Agreements.Update(ag);
    await db.SaveChangesAsync(ct);
}

        // custom UserRoleEntity nedeniyle rol atamasını manuel yap
        private static async Task EnsureUserInRoleAsync(
            IdentityDbContext db,
            UserEntity user,
            string roleName,
            CancellationToken ct)
        {
            var norm = roleName.ToUpperInvariant();

            // Rolü çek
            var role = await db.Roles.FirstOrDefaultAsync(r => r.NormalizedName == norm && !r.IsDeleted, ct);
            if (role is null)
                throw new Exception($"Role not found: {roleName}");

            // Zaten bağlı mı?
            var exists = await db.Set<UserRoleEntity>()
                .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, ct);
            if (exists) return;

            // Link oluştur (audit vb. alanlarınız varsa set edin)
            var link = new UserRoleEntity
            {
                UserId = user.Id,
                RoleId = role.Id
            };

            db.Set<UserRoleEntity>().Add(link);
            await db.SaveChangesAsync(ct);
        }
    }

}