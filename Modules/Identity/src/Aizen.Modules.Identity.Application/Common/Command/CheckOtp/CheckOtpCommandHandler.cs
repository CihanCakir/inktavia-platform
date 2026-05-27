using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Domain.Interface;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.CheckOtp
{
 public class CheckOtpCommandHandler : AizenCommandHandler<CheckOtpCommand, CheckOtpDto>
    {
        private readonly IAuthorizationService _authorizationService;

        public CheckOtpCommandHandler(IAuthorizationService    authorizationService)
        {
            _authorizationService = authorizationService;
        }

        public override async Task<CheckOtpDto> Handle(CheckOtpCommand request, CancellationToken cancellationToken)
        {
            var response = await _authorizationService.CheckOtpAsync(
                phoneNumber: request.PhoneNumber,
                otp: request.Otp,
                validationGuid: request.ValidationGuid
            );

            return new CheckOtpDto
            {
                IsConfirmed = response.IsVerified
            };
        }
    }
}