# Aizen.Modules.Content

A lightweight, marine-domain **content / promotion** bounded context — a headless CMS plus a
publishing/targeting layer — consumed by the platform's three front-end surfaces (public web, provider
panel, mobile participant app). **MongoDB-only** (no PostgreSQL / EF Core / migrations).

Status: **C0–C9 complete**, `dotnet build Aizen.sln` green, unit tests green.

---

## Content kinds & targeting

- **Types:** `Blog`, `Announcement`, `Campaign` (presentation only — links out, no pricing), `ReleaseNote`,
  `ProductPromo`, `Faq`.
- **Two orthogonal axes:** *Placement* (`Surface` × `Slot` × `Position` × `PinnedUntil`) — where it renders;
  *Audience* (`Public | Providers | VesselOwners | Segment` + region/city/tier filters) — who may see it.
- **Surfaces:** `Provider`, `MobileParticipant`, `MarineOsWeb`.
- **Engagement (content-only):** comments + favorites. Ratings live in ServiceRequest (out of scope).

## Layers (6 projects under `src/`)

| Project | Contents |
| --- | --- |
| `…Content` (host) | Program.cs, Controllers (`Controllers/V1`), rate-limit policy, seed bootstrap |
| `…Content.Abstraction` | DTOs, Enums, request/query Models, integration **Messages** — referenced by other modules |
| `…Content.Application` | CQRS Commands/Queries/Handlers/Validators + Services (mapper, slug, cache seam, validators) |
| `…Content.Core` | `ContentRoles` constants |
| `…Content.Domain` | Mongo documents (`MongoDocuments/`), value objects, repository + service interfaces |
| `…Content.Repository` | Mongo context, index initializer, repository impls, DI, demo seed |

## Endpoints

- **Admin authoring** — `api/v1/content/admin` — `[Authorize(Roles = Admin,SuperAdmin,ContentAdmin,ContentEditor)]`.
  Create/Update/UpsertTranslation/SetPlacements/SetAudience/AttachMedia/Schedule/Publish/Unpublish/Archive/Delete,
  admin List + GetById, category Create/Update. Publish/Unpublish/Archive/Delete + category mgmt are
  **narrowed in-handler** to `Admin,SuperAdmin,ContentAdmin`.
- **Comment moderation** — `api/v1/content/admin/comments` — `[Authorize(Roles = Admin,SuperAdmin,ContentAdmin,ContentModerator)]`.
  Queue (all statuses), Moderate (approve/reject/hide), Delete.
- **Participant engagement** — `api/v1/content/me` — `[Authorize]`. Comment, favorite/unfavorite,
  my favorites, my comment status. Author identity from the token only.
- **Public read** — `api/v1/content/public` — `[AllowAnonymous]` + `public-read-ip` rate limit.
  Feed, by-type, by-slug, category tree, approved comments.

Full authorization matrix: [`docs/CONTENT_KEYCLOAK_ROLES.md`](docs/CONTENT_KEYCLOAK_ROLES.md).

## MongoDB collections

| Collection | Document | Notes |
| --- | --- | --- |
| `content_items` | `ContentItemDocument` | aggregate root; embeds translations/media/placements/audience; denormalized `CommentCount`/`FavoriteCount` |
| `content_comments` | `ContentCommentDocument` | grows unbounded; moderation trail (`ModeratedByUserId`/`ModeratedAt`/`LastModerationReason`) |
| `content_favorites` | `ContentFavoriteDocument` | one live favorite per (content,user) |
| `content_categories` | `ContentCategoryDocument` | editorial taxonomy |

**Indexes:** unique-partial `Slug` (items) / `Slug` (categories) / `(ContentId,UserId)` (favorites) —
`partialFilterExpression { isDeleted:false }` so soft-deleted rows release the constraint;
`Status`, `Type`, `Placements.Surface`, `PublishAt`, `ExpireAt`, `Audience.Type`, `Tags`;
comments `(ContentId,Status,CreatedAt)` + `AuthorUserId`; favorites `(UserId,CreatedAt)`.

Enums persist as **strings** (`[BsonRepresentation(BsonType.String)]`). `PublishAt`/`ExpireAt` are
`DateTimeOffset` (BSON array) — indexed separately (a compound index over two would fail); publish-window
filtering is done **in memory** because DateTimeOffset range predicates don't translate server-side.

## Integration events

`ContentPublishedMessage` / `ContentUnpublishedMessage` (`Abstraction/Message`, `: AizenBaseMessage`) are
published best-effort on publish/unpublish/archive-if-published/delete-if-published — never failing the
write. Content does **not** dispatch notifications. Contract + consumer skeleton:
[`docs/CONTENT_NOTIFICATION_CONTRACT.md`](docs/CONTENT_NOTIFICATION_CONTRACT.md).

## Roles

`ContentAdmin`, `ContentEditor`, `ContentModerator` (+ existing `Admin`, `SuperAdmin`) — Keycloak client
roles surfaced via the Identity token → `AizenIdentityClaimsTransformation`. See the roles doc.

## Config keys

| Key | Default | Purpose |
| --- | --- | --- |
| `DatabaseSettings:ContentMongo:ConnectionString` | — | Mongo connection |
| `DistributedCache:*` | — | Redis (feed/detail cache, generation keys) |
| `Content:Comments:PreModeration` | `true` | new comments start `Pending` (`false` → `Approved`) |
| `Content:Languages:Supported` | `["tr","en"]` | translation language allowlist (B4 fallback — see below) |
| `Content:Seed:Demo` | `false` | seed a few demo items on boot (idempotent) |
| `Content:RateLimiting:PublicRead:{PermitLimit,WindowSeconds}` | `120` / `60` | per-IP public-read limit |
| `RemoteCalls:IFileStorageRemoteCall:BaseUrl` | — | FileStorage API for media validation (B3) |

## Cross-module integrations

- **FileStorage (B3):** media `FileStorageId`s validated via `IFileStorageRemoteCall.GetFileMetadata`
  (fail-closed) on create/attach.
- **ReferenceData (B4):** ReferenceData.Abstraction exposes **no** Language contract, so language codes are
  validated against the `Content:Languages:Supported` config allowlist. Repoint if ReferenceData ships a
  Language reference.

## Caching

Public reads cache through a **generation seam**: keys embed `content:gen` (+ `content:gen:{surface}`);
publish/unpublish/edit-while-published and category CRUD bump the generation, so writes invalidate reads
without key deletion. TTLs: feed/detail 5 min, category 30 min (idle backstops).

## Known limits (backlog)

- **Feed ordering** (pinned → placement position → PublishAt desc) keys off the surface-matched placement
  position, a per-item derived value not expressible as a server-side sort. The feed/by-type handlers fetch
  a **bounded candidate set (`CandidateCap = 500`)** and order/page in memory; hitting the cap is logged as a
  warning. **Backlog:** replace with a Mongo aggregation pipeline (precomputed sort key) when a surface's
  published set can exceed the cap.

## Tests

`tests/Aizen.Modules.Content.Application.UnitTests` (xUnit + FluentAssertions, in-memory fakes):
status transitions, comment-counter delta matrix, feed projection (window/surface/order/lang-fallback/paging),
slug uniqueness (soft-delete-aware), language validation, publish guards + elevated narrowing, favorite
idempotency + re-favorite-after-remove, moderation counter path + trail, media validation, public-feed
handler (window/surface + cache-hit), demo seed on/off/idempotent. Run: `dotnet test`.
