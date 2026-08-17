using Aizen.Core.Api.Middleware;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Repository.Context;
using Aizen.Modules.Payment.Abstraction;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Identity.Repository.Identity.Service
{
    public sealed class OrganizerKeycloakProvisioningDomainService : IOrganizerKeycloakProvisioningDomainService
    {
        private readonly UserManager<UserEntity> _userManager;
        private readonly RoleManager<RoleEntity> _roleManager;
        private readonly IUserProfileRepository _profileRepo;
        private readonly IProviderOnboardingDomainService _onboarding;
        private readonly IdentityDbContext _db;

        public OrganizerKeycloakProvisioningDomainService(
            UserManager<UserEntity> userManager,
            RoleManager<RoleEntity> roleManager,
            IUserProfileRepository profileRepo,
            IProviderOnboardingDomainService onboarding,
            IdentityDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _profileRepo = profileRepo;
            _onboarding = onboarding;
            _db = db;
        }

        public async Task<OrganizerKeycloakProvisionResult> ProvisionAsync(
            ProvisionOrganizerFromKeycloakDomainModel model, CancellationToken cancellationToken = default)
        {
            var warnings = new List<string>();
            var email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            var sub = model.KeycloakSubjectId.Trim();

            // 1) find by Keycloak subject
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.KeycloakSubjectId == sub, cancellationToken);

            var createdUser = false;

            if (user is null)
            {
                // 2) find by email
                var byEmail = !string.IsNullOrWhiteSpace(email)
                    ? await _userManager.FindByEmailAsync(email)
                    : null;

                if (byEmail is not null)
                {
                    if (string.IsNullOrWhiteSpace(byEmail.KeycloakSubjectId))
                    {
                        // 3) link existing (previously local) user to this Keycloak subject
                        byEmail.SetKeycloakSubjectId(sub);
                        await _userManager.UpdateAsync(byEmail);
                        user = byEmail;
                    }
                    else if (string.Equals(byEmail.KeycloakSubjectId, sub, StringComparison.Ordinal))
                    {
                        user = byEmail;
                    }
                    else
                    {
                        // 4) controlled conflict: email belongs to a different Keycloak subject
                        throw new AizenBusinessException(((int)AizenErrorCode.EmailConflictWithExistingUser).ToString());
                    }
                }
                else
                {
                    // 5) create a password-less Keycloak-backed user
                    user = UserEntity.CreateFromKeycloak(
                        email, NormalizePhone(model.ContactPhone), sub, model.EmailVerified ?? false);

                    var create = await _userManager.CreateAsync(user);
                    if (!create.Succeeded)
                        throw new AizenBusinessException(((int)AizenErrorCode.UserCreationFailed).ToString());

                    createdUser = true;
                }
            }

            // ensure Organizer role
            var role = await _roleManager.FindByNameAsync(RoleNames.Organizer)
                       ?? throw new AizenBusinessException(((int)AizenErrorCode.RoleNotFound).ToString());

            if (!await _userManager.IsInRoleAsync(user, role.Name!))
            {
                var addRole = await _userManager.AddToRoleAsync(user, role.Name!);
                if (!addRole.Succeeded)
                    throw new AizenBusinessException(((int)AizenErrorCode.RoleAssignFailed).ToString());
            }

            // ensure exactly one Organizer profile (idempotent)
            var profile = await _profileRepo.GetActiveProfileIdAsync(user.Id, WorkshopRoleContext.Organizer);
            var createdProfile = false;
            var alreadyLinked = false;

            if (profile is not null)
            {
                alreadyLinked = true;
                if (!string.IsNullOrWhiteSpace(model.CompanyName) && string.IsNullOrWhiteSpace(profile.CompanyName))
                    profile.SetCompanyName(model.CompanyName);
            }
            else
            {
                profile = UserProfileEntity.Create(
                    user.Id,
                    model.FirstName ?? string.Empty,
                    model.LastName ?? string.Empty,
                    TaxpayerType.SoleProprietorship);

                profile.RoleContext = WorkshopRoleContext.Organizer;
                if (!string.IsNullOrWhiteSpace(model.CompanyName))
                    profile.SetCompanyName(model.CompanyName);

                user.AddProfile(profile, markAsActive: false);
                await _profileRepo.AddProfileAsync(profile);
                await _userManager.UpdateAsync(user);
                createdProfile = true;
            }

            if (!string.IsNullOrWhiteSpace(model.TaxNo))
                warnings.Add("TaxNo is not persisted on the Organizer profile (no field). Value ignored.");

            await _db.SaveChangesAsync(cancellationToken);

            // A provider profile without an onboarding record is a broken aggregate: GET /onboarding returns
            // nothing and every step save fails with "Onboarding record not found". Only the legacy
            // RegisterOrganizer command created this row, so every provider who signed up through the SPA (i.e.
            // through Keycloak) was born unable to start onboarding at all.
            //
            // Idempotent: a provider provisioned before this fix gets the row on their next request.
            await _onboarding.EnsureOnboardingRowAsync(profile.Id, user.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return new OrganizerKeycloakProvisionResult(
                user, profile, createdUser, createdProfile, alreadyLinked, warnings);
        }

        private static string? NormalizePhone(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var digits = new string(input.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("0")) digits = digits[1..];
            if (!digits.StartsWith("90")) digits = "90" + digits;
            return "+" + digits;
        }
    }
}
