# Content — Keycloak Roles & Authorization Matrix

> **Ground truth.** This document reflects the authorization *as shipped* in C4–C7, not the build
> prompt's §7 aspiration. Every row was cross-checked against the actual `[Authorize(...)]` attributes,
> the `ContentRoles` constants (`Aizen.Modules.Content.Core/ContentRoles.cs`), and the in-handler
> `ContentAuthorization.EnsureElevated(...)` call sites. Where the code diverges from spec §7 it is
> flagged in [§6](#6-deviations-from-spec-7).

---

## 1. Roles to define in the Keycloak realm

| Role | New? | Purpose |
| --- | --- | --- |
| `Admin` | existing | Platform superset — full access to every Content surface. |
| `SuperAdmin` | existing | Platform superset — full access to every Content surface. |
| `ContentAdmin` | **new** | Full authoring + publish/unpublish/archive/delete + category management + moderation. |
| `ContentEditor` | **new** | Create/edit/schedule drafts, upsert translations, set placements/audience, attach media. **Cannot** publish, unpublish, archive, hard-delete, or manage categories. |
| `ContentModerator` | **new** | Approve/reject/hide/delete comments (moderation queue only). |

These are the exact string values in `ContentRoles`:

```csharp
public const string Admin            = "Admin";
public const string SuperAdmin       = "SuperAdmin";
public const string ContentAdmin     = "ContentAdmin";
public const string ContentEditor    = "ContentEditor";
public const string ContentModerator = "ContentModerator";

// Controller-level role sets
public const string AdminAuthoring = "Admin,SuperAdmin,ContentAdmin,ContentEditor";
public const string Moderation     = "Admin,SuperAdmin,ContentAdmin,ContentModerator";

// In-handler elevated narrowing (publish/unpublish/archive/delete + category mgmt)
public static readonly string[] Elevated = { Admin, SuperAdmin, ContentAdmin };
```

---

## 2. How roles reach `[Authorize(Roles = ...)]`

Content module APIs sit behind the BFF and receive **two** tokens:

1. The **Keycloak service token** in the `Authorization` header (used for the BFF-to-module hop).
2. The **Identity token** (the real application user + roles) in the **`X-Aizen-User-Token`** header.

The pipeline (identical to how `Admin`/`SuperAdmin` work today):

```
X-Aizen-User-Token (Identity token, JWT)
   │
   ▼  AizenUserInfoMiddleware  (Core.InfoAccessor)
      validates the JWT and reads:  Roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role)
   │
   ▼  AizenUserInfo.Roles  (surfaced via IAizenInfoAccessor.UserInfoAccessor.UserInfo.Roles)
   │
   ▼  AizenIdentityClaimsTransformation : IClaimsTransformation
      adds one Claim(ClaimTypes.Role, r) per role onto the ClaimsPrincipal
   │
   ▼  [Authorize(Roles = "...")]  now matches application roles (ContentAdmin, …)
```

**What Keycloak must produce:** the assigned client roles must land in the **Identity token's role
claim** (standard `role` claim → `ClaimTypes.Role` after JWT mapping) — the same claim that already
carries `Admin`/`SuperAdmin`. The Identity token is issued by the Identity module, so the requirement
is: (a) define the client roles in the realm, (b) ensure the token's role mapper includes them (client
roles → `role` claim, "Add to access token" on), (c) assign the roles to the user/group. No Content code
reads Keycloak roles directly — everything flows through `AizenUserInfo.Roles`.

Handler-side reads use the same source:
`ContentAuthorization.EnsureElevated` calls `IAizenInfoAccessor.UserInfoAccessor.UserInfo.Roles` and
checks membership in `ContentRoles.Elevated`.

---

## 3. Endpoint → role matrix (as implemented)

### 3.1 `AdminContentController` — `api/v1/content/admin`
Controller attribute: **`[Authorize(Roles = ContentRoles.AdminAuthoring)]`** = `Admin, SuperAdmin, ContentAdmin, ContentEditor`.

| Method + route | Action | Effective authorization |
| --- | --- | --- |
| `POST   /` | Create | AdminAuthoring |
| `PUT    /{id}` | Update | AdminAuthoring |
| `PUT    /{id}/translations` | UpsertTranslation | AdminAuthoring |
| `PUT    /{id}/placements` | SetPlacements | AdminAuthoring |
| `PUT    /{id}/audience` | SetAudience | AdminAuthoring |
| `PUT    /{id}/media` | AttachMedia | AdminAuthoring |
| `POST   /{id}/schedule` | Schedule | AdminAuthoring |
| `POST   /{id}/publish` | Publish | AdminAuthoring **+ in-handler `EnsureElevated`** → `Admin, SuperAdmin, ContentAdmin` |
| `POST   /{id}/unpublish` | Unpublish | AdminAuthoring **+ `EnsureElevated`** |
| `POST   /{id}/archive` | Archive | AdminAuthoring **+ `EnsureElevated`** |
| `DELETE /{id}` | Delete (soft) | AdminAuthoring **+ `EnsureElevated`** |
| `GET    /` | List (all statuses) | AdminAuthoring |
| `GET    /{id}` | GetByIdAdmin | AdminAuthoring |
| `POST   /categories` | CreateCategory | AdminAuthoring **+ `EnsureElevated`** |
| `PUT    /categories/{slug}` | UpdateCategory | AdminAuthoring **+ `EnsureElevated`** |

> **In-handler narrowing is real.** `ContentEditor` passes the controller attribute but is rejected with
> an `AizenBusinessException` inside the 6 elevated handlers. The `EnsureElevated` call sites (grep-verified):
> `PublishContentItemCommandHandler`, `UnpublishContentItemCommandHandler`, `ArchiveContentItemCommandHandler`,
> `DeleteContentItemCommandHandler`, `CreateContentCategoryCommandHandler`, `UpdateContentCategoryCommandHandler`.
> `ScheduleContentItem` is **not** narrowed (editors may schedule).

### 3.2 `AdminContentCommentsController` — `api/v1/content/admin/comments`
Controller attribute: **`[Authorize(Roles = ContentRoles.Moderation)]`** = `Admin, SuperAdmin, ContentAdmin, ContentModerator`. No in-handler narrowing (the role set is the gate).

| Method + route | Action | Authorization |
| --- | --- | --- |
| `GET    /` | Queue (all statuses, filter by content + status, paged) | Moderation |
| `POST   /{commentId}/moderate` | Approve / Reject / Hide | Moderation |
| `DELETE /{commentId}` | Delete comment (soft) | Moderation |

### 3.3 `MeContentController` — `api/v1/content/me`
Controller attribute: **`[Authorize]`** (any authenticated application user). Author identity is taken
from the token inside the handlers (`UserInfo.UserId`), never from the body.

| Method + route | Action |
| --- | --- |
| `POST   /items/{contentId}/comments` | AddComment |
| `POST   /items/{contentId}/favorite` | AddFavorite |
| `DELETE /items/{contentId}/favorite` | RemoveFavorite |
| `GET    /favorites` | MyContentFavorites |
| `GET    /items/{contentId}/my-comments` | MyCommentStatus |

### 3.4 `PublicContentController` — `api/v1/content/public`
Controller attribute: **`[AllowAnonymous]`**. Every endpoint additionally carries
**`[EnableRateLimiting("public-read-ip")]`** (per-client-IP sliding window, registered in `Program.cs`).

| Method + route | Action |
| --- | --- |
| `GET /feed` | GetPublicContentFeed |
| `GET /by-type` | GetPublicContentByType |
| `GET /items/{slug}` | GetPublicContentBySlug |
| `GET /categories` | GetPublicCategoryTree |
| `GET /items/{contentId}/comments` | GetContentComments (Approved only) |

---

## 4. Capability summary per role

| Capability | Admin / SuperAdmin | ContentAdmin | ContentEditor | ContentModerator | Authenticated | Anonymous |
| --- | :--: | :--: | :--: | :--: | :--: | :--: |
| Create / edit / schedule drafts, translations, placements, audience, media | ✅ | ✅ | ✅ | — | — | — |
| Publish / Unpublish / Archive / Delete | ✅ | ✅ | ❌ | — | — | — |
| Manage categories | ✅ | ✅ | ❌ | — | — | — |
| Admin list / get-by-id (all statuses) | ✅ | ✅ | ✅ | — | — | — |
| Moderate comments (approve/reject/hide/delete, queue) | ✅ | ✅ | — | ✅ | — | — |
| Comment / favorite / my favorites / my comment status | ✅ | ✅ | ✅ | ✅ | ✅ | — |
| Public feed / by-type / by-slug / categories / approved comments | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

(— = not applicable to that role's intended surface; the role still inherits any broader membership it holds.)

---

## 5. Provision-in-Keycloak checklist

1. **Create client roles** on the application client (the one that already defines `Admin`/`SuperAdmin`):
   `ContentAdmin`, `ContentEditor`, `ContentModerator`.
2. **Token mapper** — confirm the client-roles mapper writes them into the token's `role` claim with
   *Add to access token / ID token* enabled, so they reach the Identity token the same way `Admin` does.
3. **Assign** the roles to groups (suggested: *Content Admins* → `ContentAdmin`, *Content Editors* →
   `ContentEditor`, *Content Moderators* → `ContentModerator`) or directly to users.
4. **Verify the claim** — decode the Identity token from `X-Aizen-User-Token` and confirm the `role`
   claim array contains the assigned role(s).
5. **Verify enforcement**:
   - `ContentEditor` → `POST /api/v1/content/admin` returns 200 (create draft) but
     `POST /api/v1/content/admin/{id}/publish` returns a business error (elevated-only).
   - `ContentAdmin` → publish returns 200.
   - `ContentModerator` → `GET /api/v1/content/admin/comments` returns 200; `POST /api/v1/content/admin`
     returns 403 (not in `AdminAuthoring`).

---

## 6. Deviations from spec §7

Two places where the *shipped* code is intentionally stricter than the literal §7 wording. **The §7
permission table was treated as the source of truth; the narrower C4-scope enumeration was superseded.**

1. **Category management is elevated (Admin/SuperAdmin/ContentAdmin), not open to ContentEditor.**
   §7's C4-scope narrowing text listed only *publish + hard-delete*; but the §7 capability table assigns
   "category mgmt" to `ContentAdmin` and excludes it from `ContentEditor`. Shipped code narrows
   `CreateContentCategory`/`UpdateContentCategory` via `EnsureElevated` to honor the table. (Resolved as
   C4 ambiguity #3.)
2. **Unpublish and Archive are elevated too**, not just "hard-delete + publish."
   §7 says publish + hard-delete are narrowed in-handler; shipped code also narrows `Unpublish` and
   `Archive` because they remove live content from public surfaces — the same trust level as publish.
   This is a superset (stricter) of §7 and safe.

No endpoint is *more permissive* than §7. All narrowing is enforced in-handler with a clear
`AizenBusinessException`, verified end-to-end in C4.
