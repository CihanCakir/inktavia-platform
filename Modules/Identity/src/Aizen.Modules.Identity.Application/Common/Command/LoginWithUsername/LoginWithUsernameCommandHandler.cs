using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.LoginWithUsername;
using Microsoft.AspNetCore.Identity;
using Aizen.Core.Common.Abstraction.ViewModel;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction;

namespace Aizen.Modules.InktaviaStore.Application.Command.LoginWithUsername;

public class LoginWithUsernameCommandHandler : AizenCommandHandler<LoginWithUsernameCommand, UserLoginResponse>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly SignInManager<UserEntity> _signInManager;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IAizenInfoAccessor _infoAccessor;

    private const int ValidAttemptNumber = 2;

    public LoginWithUsernameCommandHandler(
        UserManager<UserEntity> userManager,
        SignInManager<UserEntity> signInManager,
        IUserProfileRepository userProfileRepository,
        IAuthorizationService authorizationService,
        IAizenInfoAccessor infoAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userProfileRepository = userProfileRepository;
        _authorizationService = authorizationService;
        _infoAccessor = infoAccessor;
    }

    public override async Task<UserLoginResponse?> Handle(LoginWithUsernameCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
            throw new AizenBusinessException(((int)AizenErrorCode.UsernameOrPinWrong).ToString());

        if (await _userManager.IsLockedOutAsync(user))
            throw new AizenBusinessException(((int)AizenErrorCode.LoginFailedForPasswordBlockedUser).ToString());

        var signInResult = await _signInManager.PasswordSignInAsync(request.Username, request.Pin, isPersistent: true, lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            if (user.AccessFailedCount > ValidAttemptNumber)
            {
                user.LockoutEnabled = true;
                await _userManager.UpdateAsync(user);
            }

            throw new AizenBusinessException(((int)AizenErrorCode.UserNameOrPasswordWrong).ToString());
        }

        var roles = await _userManager.GetRolesAsync(user);
        var roleContext = GetRoleContextFromAppInfo(_infoAccessor.AppInfoAccessor.AppInfo.Code);
        var activeProfile = await _userProfileRepository.GetActiveProfileIdAsync(user.Id, roleContext);

        if (activeProfile is null)
            throw new AizenBusinessException(((int)AizenErrorCode.UserHasNoActiveProfileInThisPanel).ToString());

        var loginRequest = new UserLoginRequest
        {
            UserId = user.Id,
            UserName = user.UserName,
            DeviceId = request.DeviceId,
            Email = user.Email,
            FirstName = activeProfile.FirstName,
            LastName = activeProfile.LastName,
            PhoneNumber = user.PhoneNumber,
            NotificationToken = request.NotificationToken,
            RoleContext = roleContext,
            ActiveProfileId = activeProfile.Id,
            Roles = roles.ToList()  
        };

        var loginResponse = await _authorizationService.CreateLoginToken(loginRequest);

        return loginResponse;
    }

    private WorkshopRoleContext GetRoleContextFromAppInfo(string platformCode)
    {
        return platformCode switch
        {
            "ADM" => WorkshopRoleContext.Admin,
            "ORG" => WorkshopRoleContext.Organizer,
            "VEN" => WorkshopRoleContext.VenueOwner,
            _ => WorkshopRoleContext.Participant
        };
    }
}
