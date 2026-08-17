namespace Aizen.Core.InfoAccessor.Abstraction;

/// <summary>
/// Well-known auth-related HTTP headers exchanged between BFFs and internal module APIs.
/// </summary>
public static class AizenAuthHeaders
{
    /// <summary>Legacy/admin path: Identity HS256 user JWT (carries the UserId claim).</summary>
    public const string UserToken = "X-Aizen-User-Token";

    /// <summary>
    /// Provider path: shared-secret proving the caller is a trusted BFF. When it matches
    /// <c>BffAssertion:SharedSecret</c>, the module honors the asserted identity headers below.
    /// </summary>
    public const string BffAssertion = "X-Aizen-Bff-Assertion";

    /// <summary>BFF-asserted Identity user id (numeric). Honored only with a valid <see cref="BffAssertion"/>.</summary>
    public const string AssertedUserId = "X-Aizen-User-Id";

    /// <summary>BFF-asserted provider (Organizer) profile id. Optional.</summary>
    public const string AssertedProfileId = "X-Aizen-Provider-Profile-Id";
}
