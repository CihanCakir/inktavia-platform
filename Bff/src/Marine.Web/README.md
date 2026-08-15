# Aizen.Bff.Marine.Web

The Backend-for-Frontend for the **public MarineOS website** (marineos.* SPA). A thin aggregation/adaptation
layer over the platform modules — **no database, no business logic**. It fetches from module APIs over Refit,
reshapes module DTOs into web-safe DTOs (stripping internal fields), pins the content surface, and enforces the
anonymous-vs-authenticated boundary.

Mirrors the sibling `Bff/src/Marine.Participant.Mobile` conventions (CQRS, Refit remote clients, Keycloak resource
server, identity-assertion handler).

---

## Auth model

- **Inbound (visitor → BFF):** the website SPA authenticates the user directly against **Keycloak** using
  **OIDC Authorization Code + PKCE** (the SPA holds the tokens). The BFF is a **JWT resource server** — it only
  *validates* the bearer access token (audience `marine-web-bff`), flattens `realm_access.roles` to role claims,
  and reads the `participant_profile_id` claim. There is **no BFF login/cookie-session proxy** (unlike a
  server-rendered BFF); the SPA owns the OIDC redirect/refresh.
- **Outbound (BFF → modules):** every downstream call carries a **Keycloak service token** (client_credentials,
  cached) injected by `MarineWebBffAuthDelegatingHandler`. For `/me` engagement calls, once the participant identity
  is resolved the handler *also* attaches the **BFF identity assertion** headers (`X-Aizen-Bff-Assertion` +
  `X-Aizen-User-Id`) so the module derives `UserInfo.UserId` from the verified subject — never from the request body.
- **Identity resolution:** `WebParticipantContext` (token) → `WebParticipantProfileResolver` (Identity lookup by
  Keycloak subject) → `WebIdentityHolder`. The resolver's *own* Identity call runs **before** the holder is set, so
  it carries no assertion → **no recursion**. After resolution the holder is populated → the next (engagement) call
  is asserted as that participant.

---

## Endpoint map

### Public content — `api/v1/web/content` · `[AllowAnonymous]` · rate-limited `public-read-ip`
| Method | Route | Purpose |
|---|---|---|
| GET | `feed` | MarineOsWeb feed (lang, page, pageSize) |
| GET | `by-type/{type}` | Feed narrowed to a `ContentType` |
| GET | `items/{slug}` | Localized public detail (404 → clean "not found") |
| GET | `categories` | Category list |
| GET | `items/{id}/comments` | Approved public comments (paged) |

### Participant engagement — `api/v1/web/me/content` · `[Authorize(WebAuthenticated)]`
| Method | Route | Purpose |
|---|---|---|
| POST | `items/{id}/comments` | Add a comment (author from token, never body) |
| POST | `items/{id}/favorite` | Favorite an item |
| DELETE | `items/{id}/favorite` | Un-favorite |
| GET | `favorites` | My favorites (paged) |
| GET | `items/{id}/my-comments` | My own comments (keeps own moderation status) |

### Public reference — `api/v1/web/reference` · `[AllowAnonymous]` · rate-limited `public-read-ip`
| Method | Route | Purpose |
|---|---|---|
| GET | `countries` | Country reference |
| GET | `countries/{countryCode}/cities` | Cities of a country (404 → "country not found") |
| GET | `lookups/{groupCode}` | Lookup options for a group (e.g. `VESSEL_TYPE`) |

Every controller action is thin → `IAizenCQRSProcessor.ProcessAsync` → `SetResponse` → `AizenApiResponse<T>`.

---

## Remote clients (Refit `IAizenRemoteCall`) — which module endpoints they hit

| Client | Module | Endpoints | Envelope |
|---|---|---|---|
| `IContentRemoteCall` (public) | Content | `GET /api/v1/content/public/{feed,by-type,items/{slug},categories,items/{id}/comments}` | **raw DTO** |
| `IContentRemoteCall` (`/me`) | Content | `POST/DELETE/GET /api/v1/content/me/*` | `AizenApiResponse<T>` |
| `IIdentityRemoteCall` | Identity | `GET /api/v1/identity/participant/profiles/by-subject/{sub}` | `AizenApiResponse<T>` |
| `IReferenceDataRemoteCall` | ReferenceData | `GET /api/v1/reference-data/locations/countries`, `.../{country}/cities`, `.../lookup-groups/lookup-items/{group}` | `AizenApiResponse<T>` |

**Envelope discipline:** Content *public* endpoints return the raw DTO (`Ok(dto)`) → bind `Task<T>`; Content `/me`
and all ReferenceData/Identity endpoints wrap in `AizenApiResponse<T>` → handlers unwrap `.Body`. A Refit
`ApiException` is always translated to a clean `AizenBusinessException` (404 → specific "not found", other →
generic "unavailable"); no Refit type or raw status leaks to the caller.

**Future public-directory clients (not built — no public module endpoints exist yet):** a provider/venue directory
would need `Identity`/`Vessel` public read endpoints. Grepped at W3: Identity exposes only auth endpoints as
`[AllowAnonymous]` (OTP/login/refresh) and Vessel exposes none — so no directory client was invented. Add one only
when a genuinely public module endpoint ships.

---

## Surface pinning

The BFF serves exactly one content surface: `WebContentSurface.Pinned = ContentSurface.MarineOsWeb`. The feed and
by-type handlers send it on every Content call; the web query types have **no `Surface` property**, so a caller can
never request another surface (e.g. Provider or MobileParticipant content) onto the website. The module still
enforces visibility/expiry — the BFF only passes the surface.

## Field-stripping guarantees (security-critical)

`WebContentMapper` reshapes module DTOs so the website never receives internal fields:

- **by-slug detail** (`WebContentDetailDto`) omits `AuthorUserId`, `Placements`, `Audience`, `Status`, `ExpireAt`,
  `UpdatedAt`, and the full multi-language `Translations` set (one language is resolved); media omits `FileStorageId`.
- **public comments** (`WebContentCommentDto`) omit author ids, `Status`, and the moderation trail
  (`ModeratedByUserId`/`ModeratedAt`/`LastModerationReason`).
- **my-comments** (`WebMyCommentDto`) keep the caller's **own** `Status` (so they see if it's live) but strip author
  ids and the moderator trail.
- **my-favorites** (`WebMyFavoriteDto`) strip the participant's internal `UserId`/`ProfileId`.

These are covered by `WebContentMapperTests` (behavioural + structural property-absence assertions).

---

## Configuration keys

| Section | Keys | Purpose |
|---|---|---|
| `MarineWebKeycloak` | `BaseUrl`, `Realm`, `Authority`/`MetadataAddress`, `Audience`/`BffClientId` (resource-server audience), `ClientId` (public SPA), `AdminClientId` + `AdminClientSecret` (confidential service account), `WebUserRole`, `ParticipantProfileIdAttributeName`, `ModuleAssertionSecret` | Inbound JWT validation + outbound service token + identity assertion. `TokenEndpoint`/`AuthorizeEndpoint` are derived from `BaseUrl`+`Realm`. |
| `Cors:AllowedOrigins` | `string[]` | Exact website origin(s); **no wildcard**, `AllowCredentials`. Dev fallback `http://localhost:3000` when unset. Applied under `AizenBffCors.PolicyName` **before** authentication. |
| `RateLimiting:PublicRead` | `PermitLimit` (def 120), `WindowSeconds` (def 60) | Per-client-IP sliding window for every public read (`public-read-ip`). |
| `RemoteCalls:I{X}RemoteCall:BaseUrl` | `IContentRemoteCall`, `IIdentityRemoteCall`, `IReferenceDataRemoteCall` | Downstream module host base URLs. |

Forwarded headers (`X-Forwarded-For`/`-Proto`) are honoured so the real client IP (behind the gateway) drives the
rate limiter.

---

## Ops dependencies

See [`docs/MARINE_WEB_OPS.md`](docs/MARINE_WEB_OPS.md). In short:

- `MarineWebKeycloak:ModuleAssertionSecret` **must equal** each consumed module's `BffAssertion:SharedSecret`
  (Content especially, for `/me`).
- The `marine-web-bff` Keycloak service account needs the `reference_data_read` role (for `lookups/{groupCode}`)
  and `identity_read` (for by-subject resolution).
- A `web_user` realm role + a `participant_profile_id` claim mapper on the web SPA client.
- Gateway routing to this BFF host + `RemoteCalls:*` base URLs per module.

---

## Tests

`Aizen.Bff.Marine.Web.UnitTests` (xUnit + FluentAssertions) — mapper field-stripping, surface pinning, envelope
binding, engagement identity (resolver ordering / no-recursion / anonymous rejection), Refit translation.
Run: `dotnet test Bff/src/Marine.Web/Aizen.Bff.Marine.Web.UnitTests`.
