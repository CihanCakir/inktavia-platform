# Aizen.Modules.Content — Build Prompt & Roadmap (MongoDB, .NET 9, CQRS)

> **How to use this file.** This is an execution prompt for an AI coding agent (Copilot / Claude) building the `Content` module inside the Inktavia Marine OS backend (`Aizen.sln`). It is authoritative for scope, boundaries, data model, conventions, Keycloak authorization, and the phased roadmap. Follow the existing project conventions exactly (they are quoted below from real modules: `CargoDry`, `ReferenceData`, `Notification`). Do **not** invent new base classes, DI helpers, or patterns — reuse the `Aizen.Core.*` primitives shown here. Produce complete, compile-ready files. English for all code, names, and commits.

---

## 0. Purpose & Product Scope

`Content` is a lightweight, marine-domain **content / promotion** bounded context (a headless CMS + a publishing/targeting layer). It manages editorial, promotional, and informational content authored by the platform team and consumed by the three front-end surfaces of the platform.

**Content kinds (types) the module owns:**
- **Blog** — articles / editorial posts.
- **Announcement** — platform announcements, news.
- **Campaign** — **presentation only** (banner / hero / landing copy). See boundary rule B1.
- **ReleaseNote** — application version release notes ("app version tanıtımı"), structured with app target + version + platform.
- **ProductPromo** — product promotions ("ürün tanıtımı", including CargoDry products).
- **Faq** — frequently asked questions.

**Surfaces the content targets (where it renders):**
- `Provider` — the provider (Organizer) app/panel.
- `MobileParticipant` — the mobile participant (end-user) app.
- `MarineOsWeb` — the public MarineOS marketing website.

**Engagement (this module owns it for content only):**
- **Comments** — authenticated participants can comment on content items.
- **Favorites** — authenticated participants can favorite/bookmark content items by their profile identity.
- Ratings are **out of scope** — rating/review lives in `ServiceRequest` (service ratings) and is not duplicated here.

---

## 1. Architecture Rules (must obey)

1. Modular monolith / clean modular architecture. Namespace convention: `Aizen.Modules.Content.*`.
2. Six projects per the existing layer split (already scaffolded under `Modules/Content/src`):
   - `Aizen.Modules.Content` — host (Controllers, Consumers, Program.cs, configuration).
   - `Aizen.Modules.Content.Abstraction` — public contracts (DTOs, Enums, Interfaces, integration Messages/events). Referenced by other modules.
   - `Aizen.Modules.Content.Application` — CQRS Commands/Queries/Handlers/Validators + Services + DI.
   - `Aizen.Modules.Content.Core` — cross-cutting constants (Keycloak roles, cache keys, collection names constants if shared).
   - `Aizen.Modules.Content.Domain` — Mongo documents (`MongoDocuments/`), value objects, repository interfaces (`Interface/Repository`), domain service interfaces (`Interface/Service`).
   - `Aizen.Modules.Content.Repository` — Mongo context, index initializer, repository implementations, DI, seed.
3. **Persistence: MongoDB only. No PostgreSQL, no EF Core, no migrations.** Use the `Aizen.Core.Data.Mongo` primitives exactly as `ReferenceData` and `CargoDry` Mongo code does.
4. CQRS everywhere: Command / Query / CommandHandler / QueryHandler / Validator / Request / Response(DTO) / Controller endpoint.
5. Keep module boundaries clear; do not mix responsibilities. Respect existing base classes, repository patterns, DTO patterns, controller style, DI style.
6. If a decision is unclear, choose the safest architecture-compatible option and leave a short `// NOTE:` — never a `TODO` placeholder in shipped code.
7. Replace the generated `Class1.cs` placeholders in each layer; delete them once real files exist.

---

## 2. Bounded-context Boundaries (do / don't)

- **B1 — Campaign = presentation only.** `Campaign` content stores banner/landing copy, imagery, CTA link, and an optional `ExternalRef` (e.g. `{ System="Commerce", Key="<campaignId>" }`). It must **not** hold discount rules, coupon logic, commission overrides, or price effects — those belong to `Commerce`/`Payment`. Content only links out.
- **B2 — No transient delivery.** Content does not send push/in-app notifications itself. On publish it **publishes an integration event** (`ContentPublishedMessage`) on the message bus; `Notification` may consume it and dispatch. Loose coupling only.
- **B3 — No binary storage.** Media (cover, gallery images) are referenced by `FileStorage` asset id/URL via `Aizen.Modules.FileStorage.Abstraction`. Content stores references, never bytes.
- **B4 — Languages come from ReferenceData.** Supported languages are owned by `ReferenceData` (Language). Content stores translations keyed by language code (e.g. `"tr"`, `"en"`) and may validate the code against `ReferenceData` — it must not maintain its own language master list.
- **B5 — No live geo / radius queries.** Audience may include region/city codes as a filter, but any nearby/geo discovery is `GeoDiscovery`'s future concern, never `Content`.
- **B6 — Ratings are not here.** Only comments + favorites. Service ratings remain in `ServiceRequest`.
- **B7 — Editorial taxonomy is content-owned.** Tags/categories used for editorial classification live in `Content` (they grow and change editorially). Fixed marine lookups, if referenced, come from `ReferenceData` — do not pollute `ReferenceData` with editorial tags.

---

## 3. Two Orthogonal Targeting Axes (the core model decision)

Do **not** conflate these; they are independent:

1. **Placement / Surface** — *where* it renders. A content item can appear on multiple surfaces at once, each with its own slot/position. Model as a list of placements:
   `Placement { Surface (enum), Slot (enum), Position (int), PinnedUntil (DateTimeOffset?) }`.
2. **Audience / Segment** — *who* may see it. Model as:
   `Audience { Type (enum: Public | Providers | VesselOwners | Segment), RegionCodes (string[]), CityCodes (string[]), Tiers (string[]) }`.

`ReleaseNote` additionally carries structured fields: `AppTarget` (Provider | MobileParticipant | MarineOsWeb-agnostic), `Version` (semver string), `Platform` (iOS | Android | Web | All). Force-update gating is app-config, **not** Content — release notes are informational only.

---

## 4. Reuse These Exact Core Primitives (verified from the codebase)

### 4.1 Mongo document
```csharp
using Aizen.Core.Data.Mongo.Attributes;
using Aizen.Core.Data.Mongo.Document;

[AizenCollectionInfo(CollectionName = "content_items")]
public sealed class ContentItemDocument : AizenDocumentBase
{
    // AizenDocumentBase provides: string Id (ObjectId) and bool IsDeleted (soft-delete global filter).
    // Do NOT add [BsonId]/[BsonRepresentation] — they are inherited.
    ...
}
```

### 4.2 Mongo context
```csharp
using Aizen.Core.Data.Mongo;
using Aizen.Core.Common.Abstraction.Settings;
using Microsoft.Extensions.Options;

public sealed class ContentMongoDbContext : AizenMongoContext
{
    public ContentMongoDbContext(IOptions<DatabaseSettings> option) : base(option) { }
    protected override string ConfigurationKey => "ContentMongo";
}
```
`ConfigurationKey` maps to a `DatabaseSettings` section in configuration (add a `ContentMongo` connection/db section in `appsettings.json` + env, mirroring `ReferenceDataMongo`).

### 4.3 Mongo repository access (factory + generic repo)
```csharp
using Aizen.Core.Data.Mongo;
using Aizen.Core.Data.Mongo.Repository;

public sealed class ContentItemRepository : IContentItemRepository
{
    private readonly IAizenMongoRepository<ContentItemDocument> _items;

    public ContentItemRepository(IAizenMongoRepositoryFactory<ContentMongoDbContext> factory)
        => _items = factory.GetRepository<ContentItemDocument>();

    // Available surface (as used in ReferenceData.LocationRepository):
    //   AddAsync(doc, ct), ReplaceAsync(doc, ct), FindAsync(predicate, ct) -> T?,
    //   FindManyAsync(predicate, orderBy, topCount, ct), AnyAsync(predicate?, ct).
    // Use CountAsync / DeleteAsync / GetByIdAsync if present on the interface; otherwise
    // implement soft-delete by setting IsDeleted = true and ReplaceAsync.
}
```
Query ordering example (from real code):
```csharp
orderBy: q => (IOrderedMongoQueryable<ContentItemDocument>)q.OrderByDescending(x => x.PublishAt)
```

### 4.4 Index initializer (per module)
```csharp
public sealed class ContentMongoIndexInitializer
{
    private readonly IMongoDatabase _db;
    public ContentMongoIndexInitializer(ContentMongoDbContext ctx) => _db = ctx.Database;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var items = _db.GetCollection<ContentItemDocument>(ContentMongoCollectionNames.Items);
        await items.Indexes.CreateOneAsync(new CreateIndexModel<ContentItemDocument>(
            Builders<ContentItemDocument>.IndexKeys.Ascending(x => x.Slug),
            new CreateIndexOptions { Unique = true, Name = "ux_content_slug" }), cancellationToken: ct);
        // + indexes on Status, Type, Placements.Surface, PublishAt/ExpireAt, Audience, DateKey
    }
}
```
Collection names as a `static class ContentMongoCollectionNames { public const string Items = "content_items"; ... }`.

### 4.5 CQRS
```csharp
using Aizen.Core.CQRS.Message;   // AizenCommand<TResponse>, AizenQuery<TResponse>
using Aizen.Core.CQRS.Handler;   // AizenCommandHandler<TCommand,TResponse>, AizenQueryHandler<TQuery,TResponse>

public sealed class PublishContentItemCommand : AizenCommand<PublishContentItemResponse>
{
    public string ContentId { get; set; } = default!;
}

public sealed class PublishContentItemCommandHandler
    : AizenCommandHandler<PublishContentItemCommand, PublishContentItemResponse>
{
    public override async Task<PublishContentItemResponse?> Handle(
        PublishContentItemCommand request, CancellationToken cancellationToken) { ... }
}

public sealed class GetPublicContentFeedQuery : AizenQuery<ContentFeedResponse> { ... }
public sealed class GetPublicContentFeedQueryHandler
    : AizenQueryHandler<GetPublicContentFeedQuery, ContentFeedResponse>
{
    public override async Task<ContentFeedResponse> Handle(
        GetPublicContentFeedQuery request, CancellationToken ct) { ... }
}
```
Folder layout in `Application`: `Commands/<Name>/<Name>Command.cs` + `<Name>CommandHandler.cs` (+ `<Name>Validator.cs` FluentValidation where input needs validating); `Queries/<Name>/<Name>Query.cs` + `<Name>QueryHandler.cs`. (Match the plural `Commands`/`Queries` used by CargoDry.)

### 4.6 Controllers (two styles, pick per surface)
Authorized surfaces (Admin, Provider, Participant) — inherit `AizenWebApiController`, dispatch via `IAizenCQRSProcessor`, return `AizenApiResponse<T>` via `SetResponse(...)`:
```csharp
[ApiController]
[Route("api/v1/content/admin")]
[Authorize(Roles = "Admin,SuperAdmin,ContentAdmin,ContentEditor")]
public sealed class AdminContentController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IAizenInfoAccessor  _info;
    public AdminContentController(IHttpContextAccessor http, IAizenCQRSProcessor cqrs, IAizenInfoAccessor info)
        : base(http) { _cqrs = cqrs; _info = info; }

    [HttpPost]
    public async Task<AizenApiResponse<ContentItemDto?>> Create([FromBody] CreateContentItemRequest body, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<ContentItemDto>(new CreateContentItemCommand { ... }, ct);
        return SetResponse(result);
    }
}
```
Public surface — plain `ControllerBase`, `ISender` (MediatR) or `IAizenCQRSProcessor`, `[AllowAnonymous]`, rate-limited:
```csharp
[ApiController]
[AllowAnonymous]
[Route("api/v1/content/public")]
public sealed class PublicContentController : ControllerBase
{
    private readonly ISender _sender;
    public PublicContentController(ISender sender) => _sender = sender;

    [HttpGet("feed")]
    [EnableRateLimiting("public-read-ip")] // register the policy in Program.cs / starter if not present
    public async Task<IActionResult> Feed([FromQuery] ContentSurface surface, [FromQuery] string lang = "tr",
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetPublicContentFeedQuery { Surface = surface, Lang = lang, Page = page, PageSize = pageSize }, ct));
}
```

### 4.7 Identity resolution (Keycloak / BFF)
- Module APIs sit behind the BFF. The `Authorization` header carries a **Keycloak service token**; the real application user identity + roles arrive via the Identity token header (`X-Aizen-User-Token`) and are surfaced through `IAizenInfoAccessor`.
- Application roles are injected into the principal by `AizenIdentityClaimsTransformation`, so `[Authorize(Roles = "...")]` works against app roles like `Admin`.
- Provider identity: `_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId` (asserted by the BFF via `X-Aizen-Provider-Profile-Id`).
- Participant/end-user identity (for comments/favorites): use the authenticated application user id from `_info.UserInfoAccessor.UserInfo` (`UserId`). If/when the BFF asserts a participant profile id header, treat it as additive. Store the author identity on engagement documents as `AuthorUserId` (long) plus optional `AuthorProfileId`.

### 4.8 Cache
```csharp
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;

var (hit, cached) = await _cache.TryGetAsync<ContentFeedResponse>(cacheKey, ct);
if (hit) return cached;
...
await _cache.SetAsync(result, cacheKey,
    new AizenCacheOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }, ct);
```
Cache public read paths; invalidate (or bump a generation key) on publish/update/unpublish.

### 4.9 Integration events (message bus)
```csharp
using Aizen.Core.Messagebus.Abstraction.Messages;   // AizenBaseMessage
using Aizen.Core.Messagebus.Abstraction.Senders;    // IAizenMessagePublisher

// In Aizen.Modules.Content.Abstraction/Message
public sealed class ContentPublishedMessage : AizenBaseMessage
{
    public string        ContentId { get; set; } = default!;
    public string        Slug      { get; set; } = default!;
    public string        Type      { get; set; } = default!;   // ContentType as string
    public string[]      Surfaces  { get; set; } = [];         // ContentSurface[] as strings
    public string        AudienceType { get; set; } = default!;
    public DateTimeOffset PublishedAt { get; set; }
}

// In the publish handler
await _publisher.PublishAsync(new ContentPublishedMessage { ... }, cancellationToken); // best-effort; never fail the write on bus hiccup
```

### 4.10 DI wiring (mirror existing modules)
```csharp
// Aizen.Modules.Content.Repository/DependencyInjection.cs
public static IServiceCollection AddContentRepository(this IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<IContentItemRepository,     ContentItemRepository>();
    services.AddScoped<IContentCommentRepository,  ContentCommentRepository>();
    services.AddScoped<IContentFavoriteRepository, ContentFavoriteRepository>();
    services.AddScoped<IContentCategoryRepository, ContentCategoryRepository>();
    services.AddScoped<ContentMongoIndexInitializer>();
    // IMongoClient/IMongoDatabase are NOT registered here — AddAizenMongo() in Program.cs auto-discovers ContentMongoDbContext.
    return services;
}

public static async Task SeedContentAsync(this IHost host, CancellationToken ct = default)
{
    using var scope = host.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ContentMongoIndexInitializer>().InitializeAsync(ct);
    // optional: seed a few demo content items behind a config flag
}
```
```csharp
// Program.cs (host) — Mongo-only module, no UnitOfWork/EF
var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name = "Content",
    Type = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker }
}, args);

builder.Services.AddAizenCache(builder.Configuration);
builder.Services.AddAizenMongo(builder.Configuration);   // auto-discovers ContentMongoDbContext
builder.Services.AddContentRepository(builder.Configuration);
builder.Services.AddContentApplicationServices(builder.Configuration);

var app = builder.Build();
await app.SeedContentAsync();
app.Run();
```
> Confirm the exact `AddAizenMongo` extension name/signature against `Aizen.Core.Data.Mongo` and how sibling hosts register Mongo; match it verbatim. Register the CQRS/MediatR handlers the same way sibling `Application` projects do (add a `AddContentApplicationServices` that also registers handlers/validators if the house pattern requires explicit registration).

---

## 5. Data Model (MongoDB collections)

Embed what is read together; separate what grows unbounded.

### 5.1 `content_items` — aggregate root (`ContentItemDocument`)
- `Type` (ContentType), `Slug` (unique), `Status` (ContentStatus).
- `PublishAt`, `ExpireAt` (DateTimeOffset?), `DateKey` (yyyy-MM-dd of PublishAt for range queries).
- `DefaultLanguage` (string), `Translations` (embedded list): `ContentTranslation { Lang, Title, Summary, Body(rich/markdown/html), SeoTitle, SeoDescription }`.
- `Media` (embedded list): `ContentMediaRef { FileStorageId, Url, Kind (Cover|Gallery|Thumbnail), Alt, Position }`.
- `Placements` (embedded list): `ContentPlacement { Surface, Slot, Position, PinnedUntil }`.
- `Audience` (embedded): `ContentAudience { Type, RegionCodes[], CityCodes[], Tiers[] }`.
- `Tags` (string[]), `CategorySlug` (string?).
- `Campaign` presentation block (only for Type=Campaign): `{ CtaLabel, CtaUrl, ExternalRef { System, Key } }`.
- `ReleaseNote` block (only for Type=ReleaseNote): `{ AppTarget, Version, Platform }`.
- Denormalized counters: `CommentCount`, `FavoriteCount` (maintained on engagement changes).
- Audit: `AuthorUserId`, `CreatedAt`, `UpdatedAt`, `PublishedAt`.
- Indexes: unique `Slug`; `Status`; `Type`; `Placements.Surface`; `PublishAt`/`ExpireAt`; `Audience.Type`; `Tags`.

### 5.2 `content_comments` — `ContentCommentDocument` (separate, grows unbounded)
- `ContentId`, `AuthorUserId`, `AuthorProfileId?`, `AuthorDisplayName?`, `Body`, `Status` (Pending|Approved|Rejected|Hidden), `ParentCommentId?` (optional single-level threads), `CreatedAt`, `UpdatedAt`.
- Indexes: `ContentId + Status + CreatedAt`; `AuthorUserId`.

### 5.3 `content_favorites` — `ContentFavoriteDocument` (one per user+content)
- `ContentId`, `UserId`, `ProfileId?`, `CreatedAt`.
- Unique index `ContentId + UserId` (idempotent favorite). Index `UserId + CreatedAt` for "my favorites".

### 5.4 `content_categories` — `ContentCategoryDocument` (editorial taxonomy, small)
- `Slug` (unique), `Name` (Dictionary<lang,string>), `ParentSlug?`, `Position`, `IsActive`.

---

## 6. Enums (in `Aizen.Modules.Content.Abstraction/Enum`)

- `ContentType { Blog, Announcement, Campaign, ReleaseNote, ProductPromo, Faq }`
- `ContentStatus { Draft, Scheduled, Published, Archived }`
- `ContentSurface { Provider, MobileParticipant, MarineOsWeb }`
- `ContentSlot { HomeHero, Feed, Banner, Sidebar, DedicatedPage }`
- `ContentAudienceType { Public, Providers, VesselOwners, Segment }`
- `ContentMediaKind { Cover, Gallery, Thumbnail }`
- `ContentCommentStatus { Pending, Approved, Rejected, Hidden }`
- `ReleaseNotePlatform { iOS, Android, Web, All }`

> Persist enums to Mongo as strings for readability/forward-compat (configure a global enum-as-string serializer if the Mongo core doesn't already; check how existing documents serialize `EventType`-style fields — the current code stores such values as plain strings).

---

## 7. Keycloak Authorization / Permissions

The platform enforces access with `[Authorize(Roles = "...")]` where roles are **application roles** carried in the Identity token and injected via `AizenIdentityClaimsTransformation`. Define the following **client roles in the Keycloak realm** and ensure they are mapped into the Identity-token role claim (same pipeline that surfaces `Admin`/`SuperAdmin` today):

| Role | Granted capability | Applied on |
| --- | --- | --- |
| `ContentAdmin` | Full authoring + publish + category mgmt + moderation | `AdminContentController` |
| `ContentEditor` | Create/edit/schedule drafts, upsert translations, manage placements (no hard-delete) | `AdminContentController` |
| `ContentModerator` | Approve/reject/hide comments | moderation endpoints |
| (existing) `Admin`, `SuperAdmin` | Superset of the above | all admin endpoints |

Endpoint authorization matrix:
- **Admin authoring** (`/api/v1/content/admin/**`): `[Authorize(Roles = "Admin,SuperAdmin,ContentAdmin,ContentEditor")]`. Hard-delete + publish restricted further in-handler to `Admin,SuperAdmin,ContentAdmin`.
- **Comment moderation** (`/api/v1/content/admin/comments/**`): `[Authorize(Roles = "Admin,SuperAdmin,ContentAdmin,ContentModerator")]`.
- **Participant engagement** (`/api/v1/content/me/**` — comment, favorite): `[Authorize]` (any authenticated app user); author identity from `UserInfo.UserId`.
- **Provider surface read** (`/api/v1/content/provider/**`, if the provider app shows content): `[Authorize]`, provider-scoped read only.
- **Public read** (`/api/v1/content/public/**`): `[AllowAnonymous]` + rate limiting.

Deliverable for this section: a short `docs/CONTENT_KEYCLOAK_ROLES.md` listing the new realm/client roles, their mapper config, and the endpoint matrix so the Keycloak realm can be updated in lockstep.

---

## 8. Endpoint / Feature Set (CQRS)

**Admin authoring (Commands):** `CreateContentItem`, `UpdateContentItem`, `UpsertContentTranslation`, `SetContentPlacements`, `SetContentAudience`, `AttachContentMedia`, `ScheduleContentItem`, `PublishContentItem`, `UnpublishContentItem`, `ArchiveContentItem`, `DeleteContentItem` (soft-delete), `CreateContentCategory`, `UpdateContentCategory`.
**Admin authoring (Queries):** `GetAdminContentList` (filter by type/status/surface/tag, paged), `GetContentByIdAdmin`.
**Comment moderation (Commands):** `ModerateContentComment` (approve/reject/hide), `DeleteContentComment`.
**Public read (Queries):** `GetPublicContentFeed` (surface + lang + audience + paging, cached), `GetPublicContentBySlug`, `GetPublicContentByType`, `GetPublicCategoryTree`.
**Participant engagement (Commands):** `AddContentComment`, `AddContentFavorite`, `RemoveContentFavorite`.
**Participant engagement (Queries):** `GetContentComments` (approved, paged), `GetMyContentFavorites`, `GetMyCommentStatus`.

Every command/query: dedicated `Request`/`Command`/`Query` type, `Handler`, FluentValidation `Validator` for inputs, and a mapped `Response`/`Dto`. No business logic in controllers.

---

## 9. Step-by-step Roadmap (phased, professional)

Each phase ends **buildable and green** (`dotnet build Aizen.sln`). Land phases as separate PRs. Prefix branches/commits `feat(content): ...`.

### Phase C0 — Foundation & wiring
- Fix the six `.csproj` references (Domain→Abstraction+Core; Repository→Domain+Core+Data.Mongo; Application→Domain+Abstraction+Core+CQRS; host→Application+Repository). Model refs on `CargoDry`/`ReferenceData` csprojs.
- Add `ContentMongoDbContext`, `ContentMongoCollectionNames`, `ContentMongoIndexInitializer` (empty init OK).
- Add `ContentMongo` config section (`appsettings.json` + `.env.example`), mirroring `ReferenceDataMongo`.
- Rewrite `Program.cs` per §4.10 (Mongo-only, `AddAizenMongo`, seed index init). Remove all `Class1.cs`.
- Register the module project in `Aizen.sln` if not already; confirm it builds and boots.
- **Acceptance:** solution builds; host starts; Mongo connects; empty index-init runs.

### Phase C1 — Domain documents, value objects, enums, repo interfaces
- All enums (§6), all documents + embedded value objects (§5), repository interfaces in `Domain/Interface/Repository`, domain-service interfaces in `Domain/Interface/Service` (e.g. `ISlugService`).
- **Acceptance:** Domain + Abstraction compile; documents carry `[AizenCollectionInfo]` and extend `AizenDocumentBase`.

### Phase C2 — Repository implementations + indexes
- Implement the four repositories via `IAizenMongoRepositoryFactory<ContentMongoDbContext>`; implement soft-delete (set `IsDeleted`, `ReplaceAsync`).
- Fill `ContentMongoIndexInitializer` with all indexes from §5.
- Repository DI (§4.10).
- **Acceptance:** indexes created on boot; a manual insert/read round-trips.

### Phase C3 — Abstraction contracts + integration events
- DTOs (`ContentItemDto`, `ContentFeedResponse`, `ContentCommentDto`, `ContentFavoriteDto`, `ContentCategoryDto`), request/response models, and `ContentPublishedMessage` (+ any others: `ContentUnpublishedMessage`).
- **Acceptance:** Abstraction compiles and is safe for other modules to reference.

### Phase C4 — Admin authoring write-side
- Commands + handlers + validators from §8 (authoring subset), `SetResponse`-style `AdminContentController` with the §7 role attributes.
- Slug uniqueness + status transition rules (Draft→Scheduled→Published→Archived) enforced in handlers.
- On `PublishContentItem`: set `PublishedAt`, publish `ContentPublishedMessage` (best-effort), invalidate public cache.
- **Acceptance:** full authoring lifecycle works end-to-end via HTTP; publish emits the event.

### Phase C5 — Public read-side + caching
- Public queries + `PublicContentController` (`[AllowAnonymous]` + rate limiting).
- Feed filters by surface, language (fallback to `DefaultLanguage`), audience (`Public` for anonymous), `Status=Published`, `PublishAt<=now<ExpireAt`, ordered by placement Position then PublishAt desc; Redis cache with generation-bump invalidation on publish/update.
- **Acceptance:** website/mobile/provider can each fetch a surface-scoped, localized feed; cache hit path verified.

### Phase C6 — Engagement (comments + favorites)
- Participant commands/queries; `MeContentController` (`[Authorize]`); author identity from `UserInfo.UserId`.
- New comments default to `Pending` (or `Approved` if a config flag disables pre-moderation); favorites idempotent via unique index; maintain `CommentCount`/`FavoriteCount`.
- **Acceptance:** a participant can comment + favorite + list own favorites; counters update; duplicate favorite is a no-op.

### Phase C7 — Moderation + FileStorage + ReferenceData integration
- Moderation commands + admin comment endpoints (`ContentModerator` role).
- Validate media refs against `FileStorage.Abstraction`; validate translation language codes against `ReferenceData` (soft — reject unknown codes).
- **Acceptance:** moderation flow works; invalid media/lang rejected with clear validation errors.

### Phase C8 — Keycloak roles + notification consumer contract
- Author `docs/CONTENT_KEYCLOAK_ROLES.md` (§7). Ensure role attributes match realm roles.
- Confirm `Notification` can consume `ContentPublishedMessage` (provide the message contract; wire a consumer there in a follow-up if desired — do not modify `Notification` from within `Content` beyond the shared Abstraction message).
- **Acceptance:** roles documented + enforced; event contract published for `Notification`.

### Phase C9 — Hardening, seed, tests, docs
- Rate-limit policies registered; cache TTLs tuned; soft-delete + `IsDeleted` filters verified everywhere; expiry handling; optional demo seed behind a flag.
- Unit tests for handlers (status transitions, audience/surface filtering, idempotent favorite, slug uniqueness) + minimal integration test on the public feed.
- Module README + `docs/` updated.
- **Acceptance:** tests green; `dotnet build` + basic smoke pass; module documented.

---

## 10. Conventions Checklist (enforce on every file)
- [ ] Namespace `Aizen.Modules.Content.*`, English names, `sealed` classes.
- [ ] Mongo only; `AizenDocumentBase` + `[AizenCollectionInfo]`; no `[BsonId]`; no EF/Postgres/migrations.
- [ ] CQRS via `AizenCommand`/`AizenQuery` + `AizenCommandHandler`/`AizenQueryHandler`; one folder per feature.
- [ ] Controllers thin; authorized → `AizenWebApiController` + `IAizenCQRSProcessor` + `AizenApiResponse<T>`; public → `ControllerBase` + `[AllowAnonymous]` + rate limit.
- [ ] Identity via `IAizenInfoAccessor` (`UserInfo.UserId`; `KeycloakTokenInfo.ProviderProfileId`).
- [ ] `[Authorize(Roles = ...)]` per §7 matrix.
- [ ] Cache reads with `IAizenDistributedCache`; invalidate on write.
- [ ] Integration events via `IAizenMessagePublisher` + `AizenBaseMessage`, best-effort (never fail the write).
- [ ] Media via `FileStorage`; languages via `ReferenceData`; no campaign pricing logic.
- [ ] `CancellationToken` threaded everywhere; async/await; FluentValidation for inputs.
- [ ] No `TODO`; no `Class1.cs`; complete compile-ready files.

## 11. Explicit Non-goals
- No discount/coupon/commission logic (Commerce/Payment).
- No push/in-app dispatch (Notification consumes the event).
- No binary media storage (FileStorage).
- No language master list (ReferenceData).
- No geo/radius/nearby queries (GeoDiscovery).
- No service ratings (ServiceRequest).
