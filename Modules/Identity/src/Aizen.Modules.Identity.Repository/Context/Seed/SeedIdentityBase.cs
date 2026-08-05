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

                // KeycloakSubjectId if missing (dev placeholder or from config)
                if (string.IsNullOrWhiteSpace(participant!.KeycloakSubjectId))
                {
                    var configuredSub = cfg["Seed:Participant:KeycloakSubjectId"];
                    if (string.IsNullOrWhiteSpace(configuredSub))
                    {
                        // Stable dev placeholder — M2d provisioning must replace with the real Keycloak subject id
                        configuredSub = "00000000-0000-0000-0000-participant1";
                        Console.WriteLine(
                            "[WARN] SeedIdentityBase: Participant user assigned dev-placeholder KeycloakSubjectId. " +
                            "M2d provisioning must replace it with the real Keycloak subject id of the mobile user.");
                    }

                    participant.SetKeycloakSubjectId(configuredSub);
                    await userManager.UpdateAsync(participant);
                }
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