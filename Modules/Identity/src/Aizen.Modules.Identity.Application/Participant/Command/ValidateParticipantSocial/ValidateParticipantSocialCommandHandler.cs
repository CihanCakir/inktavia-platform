using System.Security;
using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Participant.ValidateParticipantSocial;

/// <summary>
/// Validates a native Google/Apple id_token via the existing <see cref="IOAuthProviderClient"/> (JWKS +
/// iss/aud/exp; nonce only if present) and returns the verified claims. Invalid/expired/aud-mismatch →
/// clean business error (the BFF maps to 401). Never creates a user.
/// </summary>
public sealed class ValidateParticipantSocialCommandHandler
    : AizenCommandHandler<ValidateParticipantSocialCommand, ParticipantSocialValidateResult>
{
    private readonly IOAuthProviderClient _oauth;
    private readonly ILogger<ValidateParticipantSocialCommandHandler> _logger;

    public ValidateParticipantSocialCommandHandler(
        IOAuthProviderClient oauth, ILogger<ValidateParticipantSocialCommandHandler> logger)
    { _oauth = oauth; _logger = logger; }

    public override async Task<ParticipantSocialValidateResult?> Handle(
        ValidateParticipantSocialCommand request, CancellationToken ct)
    {
        var provider = (request.Provider ?? string.Empty).Trim().ToLowerInvariant();
        if (provider is not ("google" or "apple"))
            throw new AizenBusinessException(((int)AizenErrorCode.OAuthIdTokenInvalid).ToString());

        ValidatedIdTokenDto idt;
        try
        {
            idt = await _oauth.ValidateNativeIdTokenAsync(provider, request.IdToken, request.Nonce, ct);
        }
        catch (SecurityException se)
        {
            _logger.LogWarning("Native social id_token rejected. Provider={Provider} Reason={Reason}", provider, se.Message);
            throw new AizenBusinessException(((int)AizenErrorCode.OAuthIdTokenInvalid).ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Native social id_token validation failed. Provider={Provider}", provider);
            throw new AizenBusinessException(((int)AizenErrorCode.OAuthIdTokenInvalid).ToString());
        }

        // Name: from the token (Google) else the caller-supplied fullName (Apple first-authorization).
        var (firstName, lastName) = SplitName(idt.Name ?? request.FullName);

        // Apple may hide the email (private relay) — the token's (relay) email is the stable email.
        return new ParticipantSocialValidateResult
        {
            Provider = provider,
            ProviderUserId = idt.Sub,
            Email = idt.Email,
            EmailVerified = idt.EmailVerified,
            FirstName = firstName,
            LastName = lastName,
        };
    }

    private static (string?, string?) SplitName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return (null, null);
        var parts = name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1 ? (parts[0], null) : (parts[0], parts[1]);
    }
}
