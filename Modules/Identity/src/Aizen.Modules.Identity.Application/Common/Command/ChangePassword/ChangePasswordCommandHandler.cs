using System.Data.Entity;
using Aizen.Core.Api.Middleware;
using Aizen.Core.Auth.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using MiniUow;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command;
    public class ChangePasswordCommandHandler: AizenCommandHandler<ChangePasswordCommand, ChangePasswordDto>
{
    private readonly IAizenUserService _userService;
    private readonly IRepository<UserEntity> _userRepository;
    private readonly IRepository<UserLoginTokenEntity> _tokenRepository;
    private readonly UserManager<UserEntity> _userManager;
    private readonly IHttpContextAccessor _contextAccessor;

    public ChangePasswordCommandHandler(
        IAizenUserService userService,
        IUnitOfWork<IdentityDbContext> unitOfWorkForUser,
        UserManager<UserEntity> userManager, IHttpContextAccessor contextAccessor)
    {
        _userService = userService;
        _userRepository = unitOfWorkForUser.GetRepository<UserEntity>();
        _tokenRepository = unitOfWorkForUser.GetRepository<UserLoginTokenEntity>(); ;
        _userManager = userManager;
        _contextAccessor = contextAccessor;
    }

    public override async Task<ChangePasswordDto> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = Convert.ToInt32(_userService.FindUserIdFromClaimsPrinciple());

        var user = await _userRepository.FirstOrDefaultAsync(x => x.Id == userId, disableTracking: false);

        if (user is null)
            throw new AizenBusinessException(((int)AizenErrorCode.UserNotFound).ToString());

        var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);

        if (!result.Succeeded)
            throw new AizenBusinessException(((int)AizenErrorCode.AnErrorOccurred).ToString());

        await _contextAccessor.HttpContext.GetTokenAsync("Authorization");

        _userRepository.Update(user);

        var tokens = await _tokenRepository.GetAllAsync(x => x.UserId == user.Id && x.IsRevoked == false);

        if (await tokens.AnyAsync())
        {
            foreach (var entity in await tokens.ToListAsync(cancellationToken))
            {
                entity.IsRevoked = true;
                _tokenRepository.Update(entity);
            }
        }

        return new ChangePasswordDto(
            userId: user.Id
        );
    }
}
