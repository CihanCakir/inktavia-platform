using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.Marine.Web.Application.Contact.Command.SubmitWebContact;

/// <summary>
/// POST /api/v1/web/contact — forwards an untrusted contact submission to the Notification module. Shape-validated at
/// the BFF; the module re-validates, scores spam, persists, and (below threshold) notifies admins. Always returns
/// <see cref="SubmitContactResponse"/>.
/// </summary>
public sealed class SubmitWebContactCommand : AizenCommand<SubmitContactResponse>
{
    public string  Name         { get; init; } = default!;
    public string  Email        { get; init; } = default!;
    public string  Subject      { get; init; } = default!;
    public string  Message      { get; init; } = default!;
    public string? SourcePage   { get; init; }
    public string? CaptchaToken { get; init; }
    public string? Honeypot     { get; init; }

    /// <summary>Caller IP the controller resolved (X-Forwarded-For / remote IP); forwarded to the module for hashing.</summary>
    public string? ClientIp     { get; init; }
}
