# Copilot Instructions — Inktavia Marine OS / Aizen Repository

You are working inside the Inktavia Marine OS backend repository.

Use English for generated code comments, markdown documentation, reports, implementation notes, package descriptions, and commit-style summaries.

Follow the existing Aizen Framework conventions. Do not introduce unrelated architectural patterns.

---

# Global Architecture Rules

## Active modules

The currently active backend modules are:

```text
Identity
ReferenceData
Vessel
FileStorage
ServiceRequest
```

## Inactive / future modules

The following modules may exist but must be treated as inactive or future integration unless explicitly requested and backed by real contracts:

```text
Payment
Profile
Provider
Seller
Commerce
Payout
Inventory
Reports
Settings
```

Do not generate active flows depending on inactive/future modules.

## General standards

* Follow the existing Aizen Framework architecture.
* Inspect similar modules before generating code.
* Respect current namespace, folder, project reference, dependency injection, options binding, response wrapper, error handling, and validation conventions.
* Use CQRS for commands and queries where the project already follows that pattern.
* Use typed DTO/response returns. Do not return raw `object` from handlers unless the existing framework contract absolutely requires it.
* Use request models under the Abstraction layer for controller input models.
* Use DTOs under the Abstraction layer for API output models.
* Use enums under the Abstraction layer unless the current module has a different convention.
* Use FluentValidation if existing modules use it.
* Use `DocumentationInfo` on every public class, interface, command, query, handler, validator, DTO, request, controller, service, repository, mapping type, message, and RemoteCall contract if this convention exists in the repository.
* Use `IAizenInfoAccessor` or the existing user/client/device accessor pattern for current user, client, tenant, and device context.
* Use `IAizenQueryHandlerCacheable` or the existing cacheable-query contract for cacheable queries.
* Use Aizen Core/Cache abstractions for Redis/distributed cache operations.
* Do not create custom Redis clients when Aizen Core/Cache already provides a repository-standard abstraction.
* Invalidate related cache after write commands.
* Use soft delete and audit conventions from existing base entities.
* Use `DateTimeOffset` or the project-standard UTC time type consistently.
* For PostgreSQL `timestamptz`, always use UTC-compatible values.
* Do not apply broad unrelated refactoring.
* Do not break existing modules.
* Keep each change focused and buildable.

---

# Security Model — Final Decision

## Identity responsibility

Identity is the user login, user profile, user context, panel context, device context, and domain authorization authority.

Identity owns:

```text
User login
Identity access token
Identity refresh token
User profile
Panel context
Admin/customer/mobile context
Device/session context
Domain-level user permissions
```

The real user context is represented by the Identity token.

## Keycloak responsibility

Keycloak is used for service-to-service security between BFFs and internal APIs.

Keycloak owns:

```text
BFF service identity
Internal API audience
Machine-to-machine authorization
Service account roles
API access permissions
```

Keycloak is not the active browser login mechanism for the Admin Web client in the current architecture.

## Final request flow

Browser to AdminPanel BFF:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

AdminPanel BFF to internal APIs:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Meaning:

```text
Authorization header:
  proves that the caller is an authorized BFF/service.

X-Aizen-User-Token header:
  proves which real Identity user/profile/context is performing the action.
```

## Forbidden browser behavior

The browser client must not:

```text
Call the Keycloak token endpoint
Use Keycloak password grant
Use Keycloak client credentials grant
Store Keycloak username/password
Store Keycloak client secrets
Decrypt credentials in browser code
Send browser-generated Keycloak access tokens to AdminPanel BFF
Call internal module APIs directly
```

---

# AdminPanel BFF Instructions

You are working under:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

Do not create an unrelated BFF architecture.

## AdminPanel BFF responsibility

The AdminPanel BFF is responsible only for:

```text
Orchestration
Aggregation
Response shaping
Admin-specific DTO/view models
Identity auth facade
Token boundary enforcement
Server-side Keycloak service token acquisition
Aizen Core/Cache-backed service-token caching
Internal API access through AizenRemoteCall
Limited caching if the existing architecture supports it
```

The AdminPanel BFF must not contain internal module domain business logic.

Domain rules remain inside internal modules.

## AdminPanel BFF authentication boundary

The AdminPanel BFF must no longer expect browser-provided Keycloak access tokens for normal Admin Web requests.

Incoming browser requests should use:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

The AdminPanel BFF must not require this incoming browser header:

```http
Authorization: Bearer <keycloakAccessToken>
```

If legacy middleware currently requires browser-provided Keycloak bearer tokens, refactor the AdminPanel BFF authentication pipeline so Admin Web requests can be authenticated by the Identity token boundary.

Do not forward incoming browser `Authorization` headers to internal APIs.

---

# AdminPanel BFF Service Token Model

AdminPanel BFF must acquire a Keycloak service token server-side using the confidential Keycloak client:

```text
admin-panel-bff
```

Use `client_credentials` only from the server-side BFF.

Never expose the client secret to browser code.

Expected Keycloak service token request:

```http
POST /realms/inktavia-realm/protocol/openid-connect/token
Content-Type: application/x-www-form-urlencoded
```

Body:

```text
grant_type=client_credentials
client_id=admin-panel-bff
client_secret=<admin-panel-bff-secret>
```

The service token must be cached until shortly before expiry.

Do not request a new Keycloak token for every internal API call.

Recommended behavior:

```text
If expires_in = 300 seconds,
cache token for approximately 240 seconds,
or refresh 60 seconds before expiry.
```

---

# Aizen Core/Cache Token Strategy

## Mandatory discovery

Before implementing service-token caching, inspect the repository for:

```text
Core/Cache
Core/Cache/src
Aizen.Core.Cache
existing Redis cache usage
existing IAizenQueryHandlerCacheable usage
existing dependency injection patterns
existing cache key builder conventions
existing distributed lock / single-flight / GetOrCreateAsync patterns
```

Use the existing Aizen Core/Cache abstraction.

Do not create:

```text
Custom Redis client
Unrelated cache abstraction
Hardcoded StackExchange.Redis usage if Aizen Core/Cache already wraps it
Ad-hoc static in-memory token cache as the primary solution
```

If the exact cache interface name differs from the examples, adapt to the real repository convention.

## Keycloak service token cache boundary

Keycloak service token caching must be service/client/realm/audience based.

Do not store Keycloak service tokens as a one-to-one pair with Identity tokens.

Reason:

```text
Keycloak service token belongs to the BFF service identity.
Identity token belongs to the real user.
The same valid admin-panel-bff Keycloak service token can be reused for many Identity users.
```

Correct cache responsibility split:

```text
KeycloakServiceTokenCache:
  service/client/realm/audience based
  user-independent
  Redis-backed through Aizen Core/Cache

IdentitySessionOrTokenContextCache:
  user/device/session based
  separate boundary
  only implemented where it fits the real Identity contract
```

## Keycloak service token cache key

Use a deterministic cache key.

Suggested format:

```text
inktavia:admin-panel-bff:keycloak-service-token:{realmHash}:{clientId}:{audienceOrScopeHash}
```

If no explicit audience/scope is requested, use:

```text
inktavia:admin-panel-bff:keycloak-service-token:{realmHash}:{clientId}:default
```

Rules:

```text
Do not include raw Identity token values in this cache key.
Do not include raw secrets in this cache key.
Do not include raw access tokens in this cache key.
Hash long or sensitive key parts.
Keep the key stable across BFF instances.
```

## Keycloak service token cache value

Store a small DTO similar to:

```csharp
public sealed class CachedKeycloakServiceToken
{
    public string AccessToken { get; init; } = default!;
    public string TokenType { get; init; } = "Bearer";
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public DateTimeOffset RefreshAfterUtc { get; init; }
}
```

If the repository has a token protection/encryption utility, use it before storing raw access tokens in Redis.

If no token protection utility exists, document this as a security follow-up in the final report.

## Redis TTL rule

Recommended TTL:

```text
Redis TTL = token expires_in - CacheSecondsBeforeExpiry
```

Example:

```text
expires_in = 300 seconds
CacheSecondsBeforeExpiry = 60
Redis TTL = 240 seconds
```

When the cached token is missing, expired, invalid, or near expiry, request a new token.

## Token stampede protection

If Aizen Core/Cache provides distributed lock, single-flight, GetOrCreateAsync, or equivalent behavior, use it.

Goal:

```text
If 50 requests arrive while the token is expired,
only one request should call Keycloak.
Other requests should wait for or reuse the refreshed cached token.
```

If no such abstraction exists, implement the smallest repository-consistent locking approach and document it.

---

# AdminPanel BFF Service Token Provider

Create or update a service similar to:

```csharp
public interface IAdminPanelBffKeycloakServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

public sealed class AdminPanelBffKeycloakServiceTokenProvider
    : IAdminPanelBffKeycloakServiceTokenProvider
{
}
```

Responsibilities:

```text
Acquire Keycloak token using client_credentials.
Use confidential client admin-panel-bff.
Use Aizen Core/Cache for Redis-backed caching.
Reuse cached token until shortly before expiry.
Refresh automatically when expired or near expiry.
Prevent token stampede where the repository supports it.
Never expose Keycloak client secret outside server-side BFF.
Never log tokens or client secrets.
```

The provider must be registered through the existing dependency injection convention.

---

# AdminPanel BFF Internal API Forwarding Contract

Every AdminPanel BFF request to internal module APIs must send:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

The Identity token must be extracted from the incoming browser request.

The Keycloak service token must be generated by the AdminPanel BFF and served from Aizen Core/Cache when valid.

Do not forward incoming browser `Authorization` headers to internal APIs.

Do not request a fresh Keycloak token per internal API request if a valid cached token exists.

---

# AdminPanel BFF RemoteCall Rules

Internal module calls must use `AizenRemoteCall` and existing Aizen RemoteCall conventions.

Do not use:

```text
Direct EF Core DbContext access from the BFF
Internal module repositories from the BFF
Custom HttpClient wrappers if AizenRemoteCall is the repository standard
Refit clients unless the repository already uses them for this exact purpose
```

The BFF Application layer may reference active modules' Abstraction class libraries for request/response contracts.

## RemoteCall header injection

Update all AdminPanel BFF internal RemoteCall flows so they use the service-token provider.

Expected outgoing headers:

```http
Authorization: Bearer <cached-or-fresh-keycloak-service-token>
X-Aizen-User-Token: Bearer <incoming identityAccessToken>
```

If the repository has a centralized RemoteCall authorization/header provider, update that central provider.

If headers are currently passed manually to each RemoteCall method, refactor to the smallest consistent reusable approach.

Do not duplicate service-token acquisition logic across handlers.

---

# Identity Device Session Cache Boundary

Identity login remains active through AdminPanel BFF.

Expected auth endpoint family:

```http
POST /auth/login/username
POST /auth/login/phone
POST /auth/login/otp
POST /auth/otp/send
POST /auth/otp/check
POST /auth/refresh
POST /auth/password/change
```

Do not invent fake endpoints such as:

```http
POST /auth/session
POST /auth/admin/login
POST /auth/identity/refresh
```

If an endpoint does not exist in the real AdminPanel BFF or internal Identity contract, document the gap instead of inventing behavior.

## Device-aware Identity session boundary

Create a device-aware Identity token/session cache only if it fits the current Identity contract.

Use current request context and existing Aizen accessors where available:

```text
IAizenInfoAccessor
UserInfo
Client
Device
DeviceId
ClientId
ApplicationContext
```

Preferred model:

```text
Identity login remains delegated to Identity through AdminPanel BFF.
BFF can cache Identity session metadata by UserId + DeviceId + SessionId.
Do not unnecessarily persist every Identity token in Redis if the browser sends X-Aizen-User-Token on each request.
If storing tokens server-side is required, store protected/encrypted token values.
If refresh token is managed server-side, bind it to DeviceId and SessionId.
If refresh token remains browser-side, BFF should not pretend it can refresh silently without receiving a valid refresh request.
```

Suggested cache key formats:

```text
inktavia:admin-panel-bff:identity-session:{sessionId}
inktavia:admin-panel-bff:identity-session-by-device:{userIdHash}:{deviceIdHash}
inktavia:admin-panel-bff:identity-token-context:{identityAccessTokenHash}
```

Rules:

```text
Do not use raw Identity tokens as Redis keys.
Do not use raw refresh tokens as Redis keys.
Do not store raw refresh tokens unless a repository-approved protection/encryption utility exists.
Hash token values before using them as lookup keys.
```

Suggested Identity session DTO:

```csharp
public sealed class CachedAdminIdentitySession
{
    public Guid UserId { get; init; }
    public string? UserName { get; init; }
    public string? PanelContext { get; init; }
    public string? DeviceId { get; init; }
    public string? ClientId { get; init; }
    public DateTimeOffset IdentityAccessTokenExpiresAtUtc { get; init; }
    public DateTimeOffset? IdentityRefreshTokenExpiresAtUtc { get; init; }
    public string? ProtectedIdentityAccessToken { get; init; }
    public string? ProtectedIdentityRefreshToken { get; init; }
}
```

Only store `ProtectedIdentityAccessToken` and `ProtectedIdentityRefreshToken` if the repository has an approved protection/encryption mechanism.

Otherwise, store only non-sensitive metadata and document token protection as a required follow-up.

---

# Token Refresh Boundary

## Keycloak service token refresh

Keycloak service token refresh is automatic inside AdminPanel BFF.

Rules:

```text
Use admin-panel-bff client credentials.
Use Aizen Core/Cache Redis-backed token caching.
Refresh before expiry.
No browser involvement.
No Identity token coupling.
No per-request Keycloak token calls when cached token is valid.
```

## Identity token refresh

Identity token refresh must only happen through the real Identity refresh contract.

Rules:

```text
Use the real Identity refresh endpoint.
Use device information if the Identity refresh endpoint requires it.
Do not invent refresh behavior that bypasses Identity module.
Do not silently extend expired Identity tokens without Identity validation.
If BFF has access to Identity refresh token and device/session context, it may refresh server-side.
If BFF does not have refresh token, return 401 and let React call the real BFF /auth/refresh endpoint.
```

---

# AdminPanel BFF AppSettings / Environment

Use local defaults for internal services:

```text
Identity:        http://localhost:7101/api/v1
ReferenceData:  http://localhost:7104/api/v1
Vessel:         http://localhost:7105/api/v1
FileStorage:    http://localhost:7106/api/v1
ServiceRequest: http://localhost:7107/api/v1
```

Use environment variables, Kubernetes secrets, user secrets, or a secret manager for the Keycloak client secret.

Do not commit real secrets.

Suggested configuration shape:

```json
{
  "KeycloakServiceToken": {
    "Authority": "http://localhost:8080/realms/inktavia-realm",
    "TokenEndpoint": "http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token",
    "ClientId": "admin-panel-bff",
    "ClientSecret": "__FROM_SECRET__",
    "CacheSecondsBeforeExpiry": 60,
    "CacheKeyPrefix": "inktavia:admin-panel-bff:keycloak-service-token"
  },
  "InternalServices": {
    "IdentityApi": "http://localhost:7101/api/v1",
    "ReferenceDataApi": "http://localhost:7104/api/v1",
    "VesselApi": "http://localhost:7105/api/v1",
    "FileStorageApi": "http://localhost:7106/api/v1",
    "ServiceRequestApi": "http://localhost:7107/api/v1"
  }
}
```

Environment variable example:

```text
KeycloakServiceToken__ClientSecret
```

Do not commit the real Keycloak client secret.

---

# AdminPanel BFF Endpoint Audit Rules

When correcting or completing AdminPanel BFF:

1. Re-scan all active module controller files directly from source.
2. Do not rely only on existing endpoint inventory files.
3. Every discovered active endpoint relevant to AdminPanel must be represented in AdminPanel BFF or documented as intentionally excluded.
4. Identity authentication and authorization endpoints must be explicitly included.
5. ReferenceData endpoints must be fully audited and added where missing.
6. Do not activate Payment or Profile module flows.
7. BFF may orchestrate, aggregate, enrich, map and shape AdminPanel-specific responses.
8. Query and QueryHandler must not be in the same file.
9. Command and CommandHandler must not be in the same file.
10. Use typed DTO/response classes.
11. Add `DocumentationInfo` to all public classes, interfaces, commands, queries, handlers, DTOs and controllers if this convention exists.
12. Run build validation and generate reports.

---

# Internal API Authorization Instructions

Each internal API must validate the service token and Identity token separately.

## Keycloak service token validation

Internal APIs should validate:

```text
issuer
audience
azp / authorized party
resource_access client roles
token expiry
signature
```

Example for Vessel API:

```text
aud contains vessel-api
azp is admin-panel-bff, customer-panel-bff, provider-panel-bff or another allowed BFF
resource_access.vessel-api.roles contains the required role
```

## Identity token validation

Internal APIs should validate:

```text
X-Aizen-User-Token exists
Identity token is valid
UserId exists
Active profile/context exists
Panel context is allowed
Domain-level ownership or admin authorization is satisfied
Device/session context is valid where required
```

Keycloak must not replace Identity domain authorization.

Identity must remain the source of user/profile/context authorization.

---

# Keycloak Realm Instructions

Use one realm:

```text
inktavia-realm
```

## BFF confidential clients

Create or maintain these BFF service clients:

```text
admin-panel-bff
customer-panel-bff
provider-panel-bff
mobile-client-bff
```

Each BFF client should be:

```text
Client authentication: ON
Service accounts: ON
Standard flow: OFF
Direct access grants: OFF
Implicit flow: OFF
```

## API/resource clients

Create or maintain these API/resource clients:

```text
identity-api
reference-data-api
vessel-api
file-storage-api
service-request-api
notification-api
```

Define client roles under the API/resource clients.

Example roles:

```text
identity-api:
  identity.auth
  identity.read
  identity.write
  identity.admin
  identity.profile.read
  identity.profile.approve
  identity.profile.reject

reference-data-api:
  reference-data.read
  reference-data.write
  reference-data.lookup.manage
  reference-data.location.read
  reference-data.currency.manage

vessel-api:
  vessel.read
  vessel.write
  vessel.admin
  vessel.document.manage
  vessel.ownership.manage

file-storage-api:
  file.read
  file.write
  file.delete
  file.read-url.create
  file.upload-url.create
  file.visibility.manage

service-request-api:
  service-request.read
  service-request.write
  service-request.admin
  service-request.assignment.manage
  service-request.dispute.manage
  service-request.completion.manage
```

## Service account role assignment

Assign API client roles to BFF service accounts.

Example for `admin-panel-bff`:

```text
identity-api:
  identity.auth
  identity.read
  identity.write
  identity.admin
  identity.profile.approve
  identity.profile.reject

reference-data-api:
  reference-data.read
  reference-data.write
  reference-data.lookup.manage
  reference-data.currency.manage

vessel-api:
  vessel.read
  vessel.write
  vessel.admin
  vessel.document.manage
  vessel.ownership.manage

file-storage-api:
  file.read
  file.write
  file.delete
  file.read-url.create
  file.upload-url.create
  file.visibility.manage

service-request-api:
  service-request.read
  service-request.write
  service-request.admin
  service-request.assignment.manage
  service-request.dispute.manage
  service-request.completion.manage
```

Customer/mobile BFF service accounts must receive narrower permissions.

## Audience mappers

If internal APIs validate `aud`, configure Keycloak audience mappers so BFF service tokens include the target API audiences.

Expected AdminPanel BFF service token audiences:

```text
identity-api
reference-data-api
vessel-api
file-storage-api
service-request-api
```

Do not rely on broad audience values if the internal API validates a specific resource audience.

---

# React Admin Web Client Contract

The React Admin Web client must call only the AdminPanel BFF.

It must not call internal module APIs directly.

It must not call the Keycloak token endpoint.

## Browser auth model

React Admin Web must authenticate through Identity via AdminPanel BFF.

Browser requests to AdminPanel BFF must send:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

React Admin Web must not send:

```http
Authorization: Bearer <keycloakAccessToken>
```

## Required frontend environment variables

React Admin Web should require only:

```env
VITE_APP_ENV=local
VITE_APP_NAME=Inktavia Marine OS Admin
VITE_DEFAULT_LANGUAGE=tr
VITE_SUPPORTED_LANGUAGES=tr,en
VITE_ADMIN_PANEL_BFF_BASE_URL=http://localhost:<ADMIN_BFF_PORT>/api/v1/admin-panel
```

The following variables must not be required in the current architecture:

```env
VITE_KEYCLOAK_URL
VITE_KEYCLOAK_REALM
VITE_KEYCLOAK_CLIENT_ID
```

If they exist from legacy implementation, mark them optional, deprecated, or future-only.

## React client forbidden behavior

The React client must not:

```text
Use keycloak-js as active browser authentication
Use Authorization Code + PKCE for Admin Web in the current Identity-only model
Use password grant
Use client credentials
Store Keycloak credentials
Store Keycloak client secret
Decrypt credentials in browser
Store tokens in localStorage
```

Identity tokens should be memory-first.

Session storage may be used only as an explicit, documented fallback behind a removable storage adapter.

---

# Postman Documentation Instructions

You are generating Postman documentation and test collections for Inktavia Marine OS.

## Active scope

Generate artifacts for:

```text
Identity
ReferenceData
Vessel
FileStorage
ServiceRequest
AdminPanel BFF
```

Skip active test generation for:

```text
Payment
Profile
```

## Do not invent contracts

Request bodies must be derived from real request DTOs, controller action parameters, validators, and command/query contracts.

Response examples and tests must be derived from real response DTOs, action return types, handler return types, or existing captured examples.

If inference is uncertain, mark it clearly in a report instead of silently inventing.

## Postman auth models

For direct internal module API testing, Postman may still use a Keycloak token plus Identity token.

For AdminPanel BFF testing under the final browser model:

```text
React-like AdminPanel BFF requests should send:
X-Aizen-User-Token: Bearer <identityAccessToken>
```

For internal API tests:

```text
Authorization: Bearer <service or test Keycloak token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Keep local-only password grant examples only for Postman or isolated developer tooling.

Do not imply that browser applications should use password grant.

## Local service roots

```text
Identity:        http://localhost:7101
ReferenceData:  http://localhost:7104
Vessel:         http://localhost:7105
FileStorage:    http://localhost:7106
ServiceRequest: http://localhost:7107
AdminPanel BFF: http://localhost:<ADMIN_BFF_PORT>
```

Create both `*_root_url` and `*_base_url` variables where this is the existing convention.

## Output convention

Create or update `docs/postman` under every active module directory if this is the repository convention.

---

# FileStorage Module Instructions

You are working inside the Inktavia Marine OS repository.

## FileStorage projects

Use these projects:

```text
Aizen.Modules.FileStorage.Api
Aizen.Modules.FileStorage.Application
Aizen.Modules.FileStorage.Abstraction
Aizen.Modules.FileStorage.Domain
Aizen.Modules.FileStorage.Repository
```

Do not create:

```text
Aizen.Modules.FileStorage.Infrastructure
```

## FileStorage layer rules

* Keep DTOs, Requests, Enums, Clients, RemoteCall contracts and Message contracts in `Aizen.Modules.FileStorage.Abstraction`.
* Keep message contracts under `Aizen.Modules.FileStorage.Abstraction/Message`.
* Keep message consumers under `Aizen.Modules.FileStorage.Api/Consumers`.
* Keep controllers under `Aizen.Modules.FileStorage.Api/Controllers/V1`.
* Keep EF entities and Mongo documents in `Aizen.Modules.FileStorage.Domain`.
* Keep persistence, AWS S3 provider, MinIO provider if needed, repositories, services, Redis/cache helpers, seed and DI in `Aizen.Modules.FileStorage.Repository`.

## FileStorage responsibility

FileStorage owns:

```text
Upload sessions
File metadata
S3 bucket/object key management
Pre-signed upload URLs
Pre-signed read URLs
Private/public access policy
Content type validation
Extension validation
File size validation
Checksum
Soft delete
Lifecycle status
File owner relation
Processing jobs
Virus scan state
Thumbnail state
Rich metadata when required
```

Other modules only reference `FileId`.

FileStorage must not implement Vessel, ServiceProvider, CargoDry, Payment or Profile domain logic.

## Object storage decision

Production provider:

```text
AWS S3
```

Local development and local integration tests:

```text
MinIO
```

Do not implement Azure Blob unless explicitly requested later.

Secrets must not be committed into the repository.

AccessKey and SecretKey must be supplied through environment variables, secret managers, user secrets, or Kubernetes secrets.

Local MinIO default credentials may be used only in local-only `.env.example` or docker-compose examples.

## FileStorage RemoteCall integration

FileStorage synchronous server-to-server calls must use the existing Aizen RemoteCall architecture.

Do not create a new Refit client or custom external client structure.

Follow the existing pattern based on:

```csharp
using Aizen.Core.RemoteCall.Abstraction;

public interface ISendMailRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallPost("/token")]
    Task<SendLoginMailResponse> SendLoginRequest([AizenRemoteCallBody] SendLoginMailRequest request);

    [AizenRemoteCallPost("/v1/email/transactional/send")]
    Task<SendLoginMailResponse> SendMailRequest(
        [AizenRemoteCallBody] SendMailRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);
}
```

Preferred FileStorage RemoteCall location if no stronger convention exists:

```text
Aizen.Modules.FileStorage.Abstraction/RemoteCall
```

Use RemoteCall for synchronous operations:

```text
GetFileMetadata
ValidateFileOwnership
CreateReadUrl
CreateUploadSession
CompleteUploadSession
LinkFileToOwner
DeleteFile
```

Use RabbitMQ/Aizen MessageBus for asynchronous operations:

```text
FileUploadedMessage
FileProcessingRequestedMessage
FileLinkedToOwnerMessage
FileDeletedMessage
OrphanFileCleanupRequestedMessage
Virus scan
Thumbnail generation
Metadata extraction
```

---

# Vessel + FileStorage Integration Instructions

## Hard rules

* Follow the existing Aizen Framework architecture.
* Do not create `Vessel.Infrastructure`.
* Do not create `FileStorage.Infrastructure`.
* Do not create Refit clients or custom external HTTP clients for FileStorage calls.
* Use `IAizenRemoteCall` for synchronous FileStorage calls.
* Use RabbitMQ/Aizen MessageBus for asynchronous events and background side effects.
* Use `IAizenMessagePublisher` from Application command/query handlers when publishing async messages.
* Use `IAizenInfoAccessor` in Vessel command handlers to read UserInfo, Client and Device data.
* Every public class/interface/command/query/handler/validator/mapping/service/message must include `DocumentationInfo` if this convention exists.
* Command handlers must return typed DTO/response/bool only. Never return `object`.
* Vessel must not store AWS S3 bucket names, object keys, storage provider internals or permanent signed URLs.
* Vessel may store `FileId` and optional snapshot fields such as original file name, content type and file size.
* Temporary read URLs must be generated by FileStorage on demand.

## Use IAizenRemoteCall for immediate operations

```text
GetFileMetadata
ValidateFileOwnership
CreateReadUrl
LinkFileToOwner
Validate file status
Validate file category/content type
```

## Use MessageBus for async operations

```text
VesselDocumentAddedMessage
VesselDocumentUpdatedMessage
VesselDocumentRemovedMessage
VesselMediaAddedMessage
VesselMediaUpdatedMessage
VesselMediaRemovedMessage
VesselCoverMediaChangedMessage
VesselMediaSortOrderChangedMessage
Audit/notification events
Orphan cleanup requests
```

---

# ServiceRequest Module Instructions

You are working inside the Inktavia Marine OS repository.

## ServiceRequest module layers

Expected module layers:

```text
Aizen.Modules.ServiceRequest.Api
Aizen.Modules.ServiceRequest.Application
Aizen.Modules.ServiceRequest.Abstraction
Aizen.Modules.ServiceRequest.Domain
Aizen.Modules.ServiceRequest.Repository
```

If the repository uses a slightly different physical folder layout, adapt to the actual repository layout while preserving the same logical layers.

Do not introduce a separate `Infrastructure` project if the existing architecture uses a `Repository` project for persistence and dependency injection.

## ServiceRequest responsibility

ServiceRequest is the operational lifecycle module for marine service marketplace workflows:

```text
Boat Owner -> Vessel -> Service Request -> Provider Offer -> Assignment -> WorkLog -> Completion -> Owner Approval / Dispute
```

It must cover:

```text
Request
Offer
Assignment
Realtime communication
Messages
Work execution
Completion evidence
Dispute flow
Admin operations
```

## Realtime requirement

Inspect and reuse:

```text
Core/Realtime/src/Aizen.Core.Realtime
```

Use the framework's current SignalR abstractions, hubs, publishers, DI extensions, group management, user connection model, serialization, authentication, and authorization approach.

Do not invent a separate SignalR architecture if `Aizen.Core.Realtime` already provides a reusable base.

---

# Reporting and Validation

## Required validation

Run build validation appropriate to the repository:

```bash
dotnet restore
dotnet build
```

Run tests if available:

```bash
dotnet test
```

If the repository contains module-specific validation scripts, use them.

Do not fake success.

If validation fails, document the exact blocker, failing project, failing command, and error summary.

## Required reports

Generate or update relevant reports under:

```text
docs/reports/
```

Possible report files:

```text
admin-panel-bff-cache-backed-keycloak-service-token-report.md
admin-panel-bff-identity-device-session-cache-report.md
admin-panel-bff-token-refresh-boundary-report.md
admin-panel-bff-internal-remote-call-forwarding-report.md
admin-panel-bff-service-token-auth-report.md
admin-panel-bff-keycloak-service-token-boundary-report.md
admin-panel-bff-auth-pipeline-fix-report.md
admin-panel-bff-endpoint-path-audit-report.md
admin-panel-bff-final-gap-report.md
file-storage-object-storage-report.md
vessel-filestorage-integration-report.md
service-request-implementation-report.md
postman-generation-report.md
```

Each report must include:

```text
Scope
Files changed
Architecture decisions applied
Aizen Core/Cache abstractions discovered where relevant
Cache keys used where relevant
Redis TTL strategy where relevant
Token refresh strategy where relevant
Device/session handling strategy where relevant
Validation commands
Validation results
Remaining gaps
Risks / follow-ups
```

---

# Non-Negotiable Summary

* Browser clients use Identity auth only in the current model.
* Browser clients do not acquire or send Keycloak tokens.
* AdminPanel BFF acquires Keycloak service tokens server-side.
* AdminPanel BFF caches Keycloak service tokens through Aizen Core/Cache and Redis where available.
* AdminPanel BFF must not request a fresh Keycloak token on every internal API request.
* Keycloak service token cache is service/client/realm/audience based, not Identity-token based.
* Identity session/device cache is a separate boundary and must be implemented only where it fits the real Identity contract.
* AdminPanel BFF sends Keycloak service token plus Identity user token to internal APIs.
* Internal APIs validate service authorization and user/domain authorization separately.
* React/Admin Web calls only AdminPanel BFF.
* Internal module APIs are never called directly from browser clients.
* Do not store secrets or tokens in unsafe locations.
* Do not use raw tokens as Redis keys.
* Do not invent endpoints.
* Do not activate future modules without real contracts.
* Follow existing Aizen patterns.
* Keep changes focused and buildable.
