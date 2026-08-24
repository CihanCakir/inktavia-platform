using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Api.Middleware;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Payment.Abstraction;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.Identity.Repository.Identity.Service
{
public sealed class OrganizerRegistrationDomainService : IOrganizerRegistrationDomainService
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly RoleManager<RoleEntity> _roleManager;
    private readonly IAgreementRepository _agreementRepo;
    private readonly IUserProfileRepository _profileRepo;
    private readonly IUserDeviceRepository _deviceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserMessagePermissionRepository _messagePermRepo;

    public OrganizerRegistrationDomainService(
        UserManager<UserEntity> userManager,
        RoleManager<RoleEntity> roleManager,
        IAgreementRepository agreementRepo,
        IUserProfileRepository profileRepo,
        IUserDeviceRepository deviceRepo,
        IUserRepository userRepo,
        IUserMessagePermissionRepository messagePermRepo)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _agreementRepo = agreementRepo;
        _profileRepo = profileRepo;
        _deviceRepo = deviceRepo;
        _userRepo = userRepo;
        _messagePermRepo = messagePermRepo;
    }

    public async Task<(UserEntity user, UserProfileEntity profile)> RegisterOrAttachAsync(
        RegisterOrganizerDomainModel m, CancellationToken ct)
    {
        if (!m.KvkkAccepted)
            throw new AizenBusinessException(((int)AizenErrorCode.RequiredAgreementsMissing).ToString());

        var email = m.Email?.Trim().ToLowerInvariant();
        var phone = NormalizePhoneE164(m.Phone);

        // upsert user (email -> phone), parola set yoksa
        var user = !string.IsNullOrWhiteSpace(email)
            ? await _userManager.FindByEmailAsync(email)
            : null;

        if (user is null && !string.IsNullOrWhiteSpace(phone))
            user = await _userRepo.CheckUserByPhoneNumber(phone!, false);

        if (user is null)
        {
            user = UserEntity.CreateLocal(email ?? string.Empty, phone, string.Empty, LoginType.Email);
            // Kalıcı dil tercihini yeni kullanıcıda ayarla; entity normalize/doğrular.
            user.SetPreferredLanguage(m.PreferredLanguage);
            var create = await _userManager.CreateAsync(user, m.Password);
            if (!create.Succeeded)
                throw new AizenBusinessException(((int)AizenErrorCode.UserCreationFailed).ToString());
            await _userManager.UpdateAsync(user);
        }
        else if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            var addPwd = await _userManager.AddPasswordAsync(user, m.Password);
            if (!addPwd.Succeeded)
                throw new AizenBusinessException(((int)AizenErrorCode.PasswordSetFailed).ToString());
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(email) && !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                throw new AizenBusinessException(((int)AizenErrorCode.EmailConflictWithExistingUser).ToString());
            if (!string.IsNullOrWhiteSpace(phone) && user.PhoneNumber != phone)
                throw new AizenBusinessException(((int)AizenErrorCode.PhoneConflictWithExistingUser).ToString());
        }

        // tek tip profil kuralı
        var ctx = WorkshopRoleContext.Organizer;
        if (await _profileRepo.HasProfileForContextAsync(user.Id, ctx))
            throw new AizenBusinessException(((int)AizenErrorCode.UserAlreadyHasProfileOfThisType).ToString());

        // rol ata
        var role = await _roleManager.FindByNameAsync(RoleNames.Organizer)
                   ?? throw new AizenBusinessException(((int)AizenErrorCode.RoleNotFound).ToString());

        if (!await _userManager.IsInRoleAsync(user, role.Name!))
        {
            var r = await _userManager.AddToRoleAsync(user, role.Name!);
            if (!r.Succeeded)
                throw new AizenBusinessException(((int)AizenErrorCode.RoleAssignFailed).ToString());
        }

        // profil: AKTİF YAPMA (onay bekleyecek)
        var profile = UserProfileEntity.Create(user.Id, m.OwnerFirstName, m.OwnerLastName, TaxpayerType.SoleProprietorship);
        profile.RoleContext = ctx;

        user.AddProfile(profile, markAsActive: false);
        await _profileRepo.AddProfileAsync(profile);
        await _userManager.UpdateAsync(user);

        // agreements (unapproved -> approve)
        var unapproved = await _agreementRepo.GetUnapprovedAgreementsAsync(user.Id, m.RequiredAgreementTypes);
        foreach (var ag in unapproved)
            _ = await _agreementRepo.ApproveAgreementAsync(user.Id, ag);

        // device (ctx bağımsız; panel ORG ise ctx Organizer)
        if (!string.IsNullOrWhiteSpace(m.DeviceId) && m.DeviceType is not null)
        {
            await _deviceRepo.AddOrUpdateDeviceAsync(user.Id, m.DeviceId!, m.NotificationToken, m.DeviceType.Value, ctx, user.ActiveProfileId);
        }

        // message permission (push)
        var wantsPush = !string.IsNullOrWhiteSpace(m.NotificationToken);
        var perm = user.UpsertMessagePermission(MessagePermissionTypes.Notification, null, wantsPush);
        await _messagePermRepo.AddOrUpdateAsync(perm, ct);

        return (user, profile);
    }

        private static string? NormalizePhoneE164(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var digits = new string(input.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("0")) digits = digits[1..];
            if (!digits.StartsWith("90")) digits = "90" + digits;
            return "+" + digits;
        }
    }
}