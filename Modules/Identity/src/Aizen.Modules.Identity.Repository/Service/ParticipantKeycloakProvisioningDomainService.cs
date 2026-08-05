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
    /// <summary>
    /// Participant (mobile) equivalent of <see cref="OrganizerKeycloakProvisioningDomainService"/>:
    /// link/create by sub→email, ensure the Consumer role + exactly one Participant profile. No onboarding
    /// row, no company (participants are individuals).
    /// </summary>
    public sealed class ParticipantKeycloakProvisioningDomainService : IParticipantKeycloakProvisioningDomainService
    {
        private readonly UserManager<UserEntity> _userManager;
        private readonly RoleManager<RoleEntity> _roleManager;
        private readonly IUserProfileRepository _profileRepo;
        private readonly IdentityDbContext _db;

        public ParticipantKeycloakProvisioningDomainService(
            UserManager<UserEntity> userManager,
            RoleManager<RoleEntity> roleManager,
            IUserProfileRepository profileRepo,
            IdentityDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _profileRepo = profileRepo;
            _db = db;
        }

        public async Task<ParticipantKeycloakProvisionResult> ProvisionAsync(
            ProvisionParticipantFromKeycloakDomainModel model, CancellationToken cancellationToken = default)
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

            // ensure Consumer (participant) role
            var role = await _roleManager.FindByNameAsync(RoleNames.Consumer)
                       ?? throw new AizenBusinessException(((int)AizenErrorCode.RoleNotFound).ToString());

            if (!await _userManager.IsInRoleAsync(user, role.Name!))
            {
                var addRole = await _userManager.AddToRoleAsync(user, role.Name!);
                if (!addRole.Succeeded)
                    throw new AizenBusinessException(((int)AizenErrorCode.RoleAssignFailed).ToString());
            }

            // ensure exactly one Participant profile (idempotent)
            var profile = await _profileRepo.GetActiveProfileIdAsync(user.Id, WorkshopRoleContext.Participant);
            var createdProfile = false;
            var alreadyLinked = false;

            if (profile is not null)
            {
                alreadyLinked = true;
            }
            else
            {
                profile = UserProfileEntity.Create(
                    user.Id,
                    model.FirstName ?? string.Empty,
                    model.LastName ?? string.Empty,
                    TaxpayerType.Individual);

                profile.RoleContext = WorkshopRoleContext.Participant;

                user.AddProfile(profile, markAsActive: true);
                await _profileRepo.AddProfileAsync(profile);
                await _userManager.UpdateAsync(user);
                createdProfile = true;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new ParticipantKeycloakProvisionResult(
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
