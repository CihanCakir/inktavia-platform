using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Common.Warnings;
using Aizen.Bff.MarineProvider.Application.Contracts.Phone;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Phone.SendProviderPhoneOtp;

public sealed class SendProviderPhoneOtpCommandHandler
    : AizenCommandHandler<SendProviderPhoneOtpCommand, SendProviderPhoneOtpResponse>
{
    private readonly IProviderContext _context;
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly ILogger<SendProviderPhoneOtpCommandHandler> _logger;

    public SendProviderPhoneOtpCommandHandler(
        IProviderContext context,
        IProviderIdentityRemoteCall identity,
        ILogger<SendProviderPhoneOtpCommandHandler> logger)
    {
        _context = context;
        _identity = identity;
        _logger = logger;
    }

    public override async Task<SendProviderPhoneOtpResponse?> Handle(
        SendProviderPhoneOtpCommand request, CancellationToken cancellationToken)
    {
        var response = new SendProviderPhoneOtpResponse();

        if (!_context.IsAuthenticated)
        {
            response.Message = "Authentication required.";
            return response;
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            response.Message = "Phone number is required.";
            return response;
        }

        try
        {
            var result = await _identity.SendOtp(new ProviderSendOtpRequest { PhoneNumber = request.PhoneNumber });
            var dto = result?.Body;
            response.ValidationGuid = dto?.ValidationGuid;
            response.ExpiredDateTime = dto?.ExpiredDateTime;
            response.ExpireCounter = dto?.ExpireCounter ?? 0;
            response.Message = "Verification code sent.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Identity SendOtp failed.");
            response.Message = "Verification code could not be sent. Please try again.";
            response.Warnings.Add(ProviderBffWarning.CallFailed("Identity.SendOtp", ex.GetType().Name));
        }

        return response;
    }
}
