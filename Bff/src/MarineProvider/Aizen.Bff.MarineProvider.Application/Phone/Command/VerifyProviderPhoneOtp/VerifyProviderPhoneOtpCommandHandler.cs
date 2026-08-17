using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Common.Warnings;
using Aizen.Bff.MarineProvider.Application.Contracts.Phone;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Phone;

public sealed class VerifyProviderPhoneOtpCommandHandler
    : AizenCommandHandler<VerifyProviderPhoneOtpCommand, VerifyProviderPhoneOtpResponse>
{
    private readonly IProviderContext _context;
    private readonly IIdentityRemoteCall _identity;
    private readonly IProviderProfileResolver _resolver;
    private readonly ILogger<VerifyProviderPhoneOtpCommandHandler> _logger;

    public VerifyProviderPhoneOtpCommandHandler(
        IProviderContext context,
        IIdentityRemoteCall identity,
        IProviderProfileResolver resolver,
        ILogger<VerifyProviderPhoneOtpCommandHandler> logger)
    {
        _context = context;
        _identity = identity;
        _resolver = resolver;
        _logger = logger;
    }

    public override async Task<VerifyProviderPhoneOtpResponse?> Handle(
        VerifyProviderPhoneOtpCommand request, CancellationToken cancellationToken)
    {
        var response = new VerifyProviderPhoneOtpResponse();

        if (!_context.IsAuthenticated)
        {
            response.Message = "Authentication required.";
            return response;
        }

        try
        {
            var result = await _identity.CheckOtp(new ProviderCheckOtpRequest
            {
                PhoneNumber = request.PhoneNumber,
                Otp = request.Otp,
                ValidationGuid = request.ValidationGuid
            });

            response.IsConfirmed = result?.Body?.IsConfirmed ?? false;
            response.Message = response.IsConfirmed ? "Phone verified." : "Invalid or expired code.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Identity CheckOtp failed.");
            response.Message = "Verification could not be completed. Please try again.";
            response.Warnings.Add(ProviderBffWarning.CallFailed("Identity.CheckOtp", ex.GetType().Name));
        }

        // On success, persist phoneVerified on the resolved Organizer profile.
        if (response.IsConfirmed)
        {
            var resolution = await _resolver.ResolveAsync(cancellationToken);
            if (resolution.ProfileId is { } profileId && profileId > 0)
            {
                try
                {
                    var mark = await _identity.MarkPhoneVerified(profileId);
                    response.PhoneVerifiedPersisted = mark?.Body?.PhoneVerified ?? false;
                    if (!response.PhoneVerifiedPersisted)
                        response.Warnings.Add(ProviderBffWarning.CallFailed("Identity.MarkPhoneVerified", "not persisted"));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Identity MarkPhoneVerified failed for {ProfileId}.", profileId);
                    response.Warnings.Add(ProviderBffWarning.CallFailed("Identity.MarkPhoneVerified", ex.GetType().Name));
                }
            }
            else
            {
                response.Warnings.Add(ProviderBffWarning.Gap(
                    "Provider",
                    "Phone verified but no linked provider profile was resolved to persist it."));
            }
        }

        return response;
    }
}
