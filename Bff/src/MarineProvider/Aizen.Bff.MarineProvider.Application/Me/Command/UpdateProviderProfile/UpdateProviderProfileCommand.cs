using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Bff.MarineProvider.Application.Me;

/// <summary>PUT /api/v1/provider/me/profile — the provider edits its own profile (business location + per-km rate,
/// plus the other self-service fields). Owner is the asserted caller; the module resolves the profile from the token.</summary>
public sealed class UpdateProviderProfileCommand : AizenCommand<UpdateProviderProfileResponse>
{
    public UpdateProviderProfileCommand(UpdateOrganizerProfileRequest body) => Body = body;

    public UpdateOrganizerProfileRequest Body { get; }
}

public sealed class UpdateProviderProfileResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
