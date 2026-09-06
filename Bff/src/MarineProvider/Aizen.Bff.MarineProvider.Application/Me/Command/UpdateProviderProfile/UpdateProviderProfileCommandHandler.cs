using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Me;

/// <summary>
/// Resolve the caller (sets the identity holder so the S2S assertion stamps UserInfo.UserId) → forward the update
/// to the Identity module, which resolves the active Organizer profile from the token and applies the change.
/// </summary>
public sealed class UpdateProviderProfileCommandHandler
    : AizenCommandHandler<UpdateProviderProfileCommand, UpdateProviderProfileResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<UpdateProviderProfileCommandHandler> _logger;

    public UpdateProviderProfileCommandHandler(
        IProviderProfileResolver resolver, IIdentityRemoteCall identity, ILogger<UpdateProviderProfileCommandHandler> logger)
    {
        _resolver = resolver;
        _identity = identity;
        _logger = logger;
    }

    public override async Task<UpdateProviderProfileResponse?> Handle(UpdateProviderProfileCommand request, CancellationToken ct)
    {
        var body = request.Body ?? throw new AizenBusinessException("Profile payload is required.");

        var resolution = await _resolver.ResolveAsync(ct);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No provider profile is linked to this account yet.");

        var resp = await _identity.UpdateOrganizerProfile(body);
        var ok = resp?.Body?.Success ?? false;
        if (!ok)
            _logger.LogWarning("Provider profile update returned non-success for profile {ProfileId}.", resolution.ProfileId);

        return new UpdateProviderProfileResponse { Success = ok, Message = resp?.Body?.Message ?? (ok ? "OK" : "Update failed.") };
    }
}
