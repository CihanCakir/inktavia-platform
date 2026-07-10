# Provider UserInfo / Identity Propagation — Decision & Plan (Phase 32 prep)

> **Status: FOUNDATION IMPLEMENTED (Phase 32, first step).** The enabling infrastructure is now in the codebase
> (module side + BFF side). Config wiring + runtime smoke are the remaining steps (build locally / Claude Code).
> Related: `marine-provider-architecture-overview.md` (§3 token strategy).

> ### Update — chosen mechanism = **shared secret** (not azp-from-principal)
> During implementation we found that `AizenApiApplicationConfiguration` runs `UseUserInfoMiddleware()` **before**
> `UseAuthentication()`, and `AizenIdentityClaimsTransformation` depends on `UserInfo` being set during
> authentication — so `AizenUserInfoMiddleware` **must** stay before auth and **cannot** read a validated
> `azp` from `HttpContext.User`. Reordering the shared pipeline is therefore unsafe. Instead we gate the
> assertion on a **shared secret** (`X-Aizen-Bff-Assertion` == `BffAssertion:SharedSecret`) plus the presence of
> a Keycloak service token, with an optional `AllowedClientIds` (azp from the raw token, defense-in-depth).
> **Safe default:** when `BffAssertion:SharedSecret` is empty the feature is a no-op — behaves exactly as before.
>
> **Implemented (this step):**
> - Core.InfoAccessor.Abstraction: `AizenAuthHeaders`, `AizenBffAssertionOptions`, `AizenKeycloakTokenInfo.ProviderProfileId`.
> - `AizenUserInfoMiddleware.TryAcceptBffAssertion(...)` (shared-secret gated, constant-time compare) + options binding in `AddAizenInfoAccessor`.
> - MarineProvider BFF: `IProviderIdentityHolder` (scoped) populated by `ProviderProfileResolver`; `MarineProviderBffAuthDelegatingHandler`
>   injects `X-Aizen-Bff-Assertion` + `X-Aizen-User-Id` (+ profile id) — recursion-safe (holder set only after the resolver's own Identity calls).
> - `MarineProviderKeycloakOptions.ModuleAssertionSecret`; docker-compose env (`BffAssertion__SharedSecret` on identity-api, `MarineProviderKeycloak__ModuleAssertionSecret` on bff-marineprovider; both from `AIZEN_BFF_ASSERTION_SECRET`).
>
> **Remaining (Claude Code / Phase 32 next):** build all affected projects; set `AIZEN_BFF_ASSERTION_SECRET`;
> add `BffAssertion__SharedSecret` to the other modules (ServiceRequest/CargoDry/Notification) as their provider
> reads land; and smoke-verify once the first operational provider endpoint exists. §6 skeleton below is the
> earlier azp variant — the shipped code uses the shared-secret gate described here.

## 1. Problem (grounded)

`Core/InfoAccessor/.../AizenUserInfoMiddleware.cs` populates `IAizenUserInfoAccessor.UserInfo`:
- `UserInfo.UserId` is set **only** from the `X-Aizen-User-Token` header (an Identity HS256 JWT carrying a
  `UserId` claim + `RefreshTokenExpire` claim).
- The `Authorization` Keycloak token is detected as a **client token** (`IsKeycloakClientToken`: valid JWT
  without a `UserId` claim) and stored as `AizenKeycloakTokenInfo { Subject=sub, ClientId=azp }`.
- If `X-Aizen-User-Token` is missing → `UserInfo = new AizenUserInfo()` → **`UserId = 0`**.

In the MarineProvider (Keycloak-first) flow the BFF forwards **only** the service token in `Authorization` and
**does not** forward an `X-Aizen-User-Token` (there is no legacy Identity user token, and we never fabricate one).
Therefore, in provider calls:
- `UserInfo.UserId == 0`.
- `AizenKeycloakTokenInfo.Subject` == the **service account** sub (`service-account-provider-portal-bff`), **not**
  the provider's sub. The module cannot identify the calling provider from the token alone.

**Why admin works but provider does not:** Admin Web authenticates via Identity and forwards its Identity JWT as
`X-Aizen-User-Token`, so modules get the admin's `UserId`. The provider flow has no such token.

## 2. Impact (Phase 32 surfaces)

Existing module handlers scope by `_info.UserInfoAccessor.UserInfo.UserId`:
- **ServiceRequest: 27 files** (offers, work logs, assignments, completion, messages, …)
- **CargoDry: 10 files**
- **Notification: 1 file**

Some also forward `UserInfo.AccessToken` to another module (e.g. `AcceptServiceRequestOffer` → Payment) — this
would be empty in the provider flow.

Phase 31 provider-link Identity endpoints (provision / by-subject / mark-verified / suspend / reactivate)
**do not** use `UserInfo.UserId` — they take identity explicitly — so 31C is unaffected. The gap is strictly
Phase 32 (reusing existing operational handlers).

## 3. Options considered

| Option | Idea | Pros | Cons |
|---|---|---|---|
| **A — Explicit params** | New/overloaded provider-safe module endpoints that take `providerProfileId`/`providerUserId` and scope by it (not by `UserInfo.UserId`). | No middleware/trust change; explicit; no legacy token. | New endpoint per module; can't reuse the 27+10+1 existing `UserId`-based handlers as-is. |
| **B — Service-token-gated BFF identity assertion** | Extend `AizenUserInfoMiddleware`: when `Authorization` is a **validated** Keycloak service token whose `azp` is in a trusted-BFF allowlist and `X-Aizen-User-Token` is absent, populate `UserInfo.UserId` from a BFF-set `X-Aizen-User-Id` header. | Reuses all existing handlers unchanged; one central change; not a fabricated JWT. | Touches shared Core middleware (affects admin + all modules); trust must be gated correctly. |
| **C — Token exchange** | BFF asks Identity for a short-lived Identity user token, forwards as `X-Aizen-User-Token`. | Everything works unchanged. | Reintroduces the legacy HS256 path + exchange endpoint + key handling; the "bridge" we rejected. |

## 4. DECISION

- **Primary mechanism: Option B** — a service-token-gated, BFF-asserted identity header, to reuse the existing
  operational handlers without rewriting them.
- **For genuinely new provider-safe endpoints: Option A** — pass `providerProfileId` explicitly; do not depend on
  the assertion header.
- **Option C is rejected** (no legacy human-token path for Provider Web).

Rationale: 27+10+1 existing handlers already encode the correct domain logic keyed on the caller's Identity
`UserId`. Option B lets the BFF (which already resolves the provider's `providerProfileId` and, via the Organizer
profile, `UserId`) assert that identity centrally, while keeping admin untouched and avoiding a fake JWT.

## 5. Security rules (mandatory for Option B)

1. **Read `azp` from the validated principal**, not from a raw `ReadToken`. The assertion is honored only after
   the ASP.NET auth pipeline has validated the Keycloak token (signature/audience). → **`UseAuthentication` must
   run before `AizenUserInfoMiddleware`.**
2. **Allowlist** the trusted BFF client ids (`provider-portal-bff`, `admin-panel-bff`). Any other `azp` → ignore
   the assertion header entirely.
3. Honor the assertion header **only when `X-Aizen-User-Token` is absent** (admin flow keeps using the user token).
4. Modules must be reachable **only via the BFF/gateway** — end users must not call modules directly.
5. Runtime authority stays with **Identity ApprovalStatus/ProfileStatus** (BFF `ProviderActive` policy). The
   assertion header conveys "who", never "may access".

## 6. Code skeleton (ILLUSTRATIVE — DO NOT IMPLEMENT NOW)

### 6.1 Header constants + trusted-BFF options (Core.InfoAccessor)
```csharp
public static class AizenAuthHeaders
{
    public const string UserToken        = "X-Aizen-User-Token";        // existing (admin/Identity)
    public const string AssertedUserId   = "X-Aizen-User-Id";           // NEW: BFF-asserted Identity user id
    public const string AssertedProfileId= "X-Aizen-Provider-Profile-Id"; // NEW: optional
}

public sealed class AizenTrustedBffOptions
{
    public const string SectionName = "TrustedBff";
    // azp values allowed to assert identity via headers (validated principal only)
    public string[] AllowedClientIds { get; set; } = new[] { "provider-portal-bff", "admin-panel-bff" };
}
```

### 6.2 Middleware extension (only the added branch shown)
```csharp
// ... after IsKeycloakClientToken(...) captured AizenKeycloakTokenInfo ...

// If no Identity user token, try a BFF-asserted identity — but ONLY if the caller is a trusted,
// VALIDATED service client (azp from httpContext.User, not from raw ReadToken).
if (!httpContext.Request.Headers.TryGetValue(AizenAuthHeaders.UserToken, out var userTokenHeader)
    || string.IsNullOrWhiteSpace(userTokenHeader))
{
    if (TryAcceptBffAssertion(httpContext, out var asserted))   // NEW
    {
        container.Set(asserted);
        await _next(httpContext);
        return;
    }

    container.Set(new AizenUserInfo());   // unchanged fallback (UserId = 0)
    await _next(httpContext);
    return;
}
// ... existing X-Aizen-User-Token path unchanged ...

private bool TryAcceptBffAssertion(HttpContext ctx, out AizenUserInfo info)
{
    info = null;

    // azp MUST come from the authenticated principal (token already validated by UseAuthentication).
    var azp = ctx.User?.FindFirst("azp")?.Value;
    if (string.IsNullOrWhiteSpace(azp)) return false;

    var trusted = ctx.RequestServices.GetRequiredService<IOptions<AizenTrustedBffOptions>>().Value;
    if (!trusted.AllowedClientIds.Contains(azp, StringComparer.OrdinalIgnoreCase)) return false;

    var uidStr = ctx.Request.Headers[AizenAuthHeaders.AssertedUserId].FirstOrDefault();
    if (!long.TryParse(uidStr, out var uid) || uid <= 0) return false;

    var roles = ctx.User.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToArray();

    info = new AizenUserInfo { UserId = uid, Roles = roles, AccessToken = string.Empty };
    return true;
    // Optional: also stash X-Aizen-Provider-Profile-Id into AizenKeycloakTokenInfo/UserInfo if a field is added.
}
```
> Note: this requires `UseAuthentication()` to run before this middleware in each module's pipeline. Verify the
> ordering in the module host configuration during implementation.

### 6.3 MarineProvider BFF — set the assertion headers (AuthDelegatingHandler)
```csharp
// In MarineProviderBffAuthDelegatingHandler.SendAsync, after injecting the service token:
var resolver = ctx.RequestServices.GetRequiredService<IProviderProfileResolver>();
var resolution = await resolver.ResolveAsync(cancellationToken);   // gives Profile (UserId) + ProfileId
if (resolution.Profile is { } p && resolution.ProfileId is > 0)
{
    if (!request.Headers.Contains(AizenAuthHeaders.AssertedUserId))
        request.Headers.TryAddWithoutValidation(AizenAuthHeaders.AssertedUserId, p.UserId.ToString());
    if (!request.Headers.Contains(AizenAuthHeaders.AssertedProfileId))
        request.Headers.TryAddWithoutValidation(AizenAuthHeaders.AssertedProfileId, resolution.ProfileId.Value.ToString());
}
// Still NO X-Aizen-User-Token. Still NO fabricated Identity JWT.
```
> `OrganizerProfileDetailDto.UserId` already exists, so the resolver already carries the Identity `UserId`.
> Consider caching the resolution per request to avoid repeated Identity lookups.

## 7. Cross-module `AccessToken` chains

Handlers that forward `UserInfo.AccessToken` to another module (e.g. `AcceptServiceRequestOffer` → Payment) will
see an empty token in the provider flow. Per-endpoint options in Phase 32:
- switch the downstream call to the **service token** (machine-to-machine), or
- pass the needed identity **explicitly** as a parameter (Option A), or
- if the operation is not part of the provider MVP, leave it admin/customer-only for now.
Audit each such chain when the corresponding provider endpoint is built.

## 8. Phase 32 implementation checklist (later)

1. Add `AizenAuthHeaders` + `AizenTrustedBffOptions` (Core.InfoAccessor.Abstraction) and bind `TrustedBff` config.
2. Extend `AizenUserInfoMiddleware` with `TryAcceptBffAssertion` (validated-principal azp allowlist).
3. Confirm/adjust module pipeline so `UseAuthentication` precedes `AizenUserInfoMiddleware`.
4. Set the assertion headers in `MarineProviderBffAuthDelegatingHandler` from the resolved provider profile.
5. (Optional) extend `AizenUserInfo`/`AizenKeycloakTokenInfo` with `ProviderProfileId` for direct scoping.
6. Audit the `UserInfo.AccessToken` forwarding chains (§7).
7. Prefer Option A (explicit `providerProfileId`) for any brand-new provider-safe module endpoint.
8. Tests: (a) provider token → module handler sees correct `UserId`; (b) spoofed `X-Aizen-User-Id` from a
   non-allowlisted `azp` is ignored; (c) admin flow (`X-Aizen-User-Token`) unchanged.

## 9. What stays true regardless

- No fabricated `X-Aizen-User-Token`; no legacy HS256 human token for Provider Web.
- BFF never calls AdminPanel BFF; modules reachable only via BFF/gateway.
- Identity runtime status remains the authoritative access gate.
