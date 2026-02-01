using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.SendOtp;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command
{
    public class SendOtpCommandHandler : AizenCommandHandler<SendOtpCommand, SendOtpDto>
    {
        private readonly IAuthorizationService _authorizationService;

        public SendOtpCommandHandler(IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }

        public override async Task<SendOtpDto> Handle(SendOtpCommand request, CancellationToken cancellationToken)
        {
            return await _authorizationService.SendOtpAsync(request.PhoneNumber);
        }
    }
}