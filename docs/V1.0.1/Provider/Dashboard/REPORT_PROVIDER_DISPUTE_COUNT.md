# REPORT — provider-facing dispute count/list (unblocks the dashboard "disputes" attention row)

> Executes `FIX_PROVIDER_DISPUTE_COUNT.md`. ServiceRequest module + MarineProvider BFF + `inktavia-marine-provider-web`.
> Additive, cost-free, no economics change. Closes the last P-QA1 attention-strip gap. **Not committed.**

---

## What was missing

Disputes were owner/admin-oriented: `GetAdminDisputeList` (admin) and `GetDisputeCaseDetail(disputeId)` (admin case
file). There was **no provider "my disputes" list or count**, so P-QA1 intentionally omitted the dashboard "disputes
needing input" attention row (`DashboardPage.tsx` even carried a comment saying disputes had "no provider-side count
source yet, so it is intentionally omitted"). This adds the missing provider-scoped surface and lights up the row.

## The party link (how "my disputes" is defined)

A `ServiceRequestDisputeEntity` stores only its `ServiceRequestId` — never a provider id. The provider party is derived
from the SR's **accepted offer**, exactly as `GetDisputeCaseDetail` does (`sr.Offers.First(Accepted).ProviderUserId`).
A dispute only exists post-acceptance, so "my disputes" = disputes on the service requests this provider **won**
(`ServiceRequestOffers` where `ProviderProfileId == me && Status == Accepted`). Open/actionable = status **not**
Resolved/Closed (i.e. Open, UnderReview, PendingOwnerResponse, PendingProviderResponse, Escalated).

## SR module (new)

- **`Abstraction/Response/Provider/GetProviderDisputesResponse.cs`** — `GetProviderDisputesResponse { Items, PageIndex,
  PageSize, Total, OpenCount }` + `ProviderDisputeItemDto`. **Cost-free** (§20.9): dispute + SR header only
  (`DisputeId, ServiceRequestId, ServiceRequestCode/Title, ServiceCategoryCode, Status, Reason, Description, IsOpen,
  OpenedByMe, OpenedAt, ResolvedAt`) — no supplier cost / dealer margin / offer economics. `Status`/`Reason` cross as
  enum **names** (strings) so the FE needs no numeric-enum mapping.
- **`Application/Query/Provider/GetProviderDisputes/GetProviderDisputesQuery.cs`** — `AizenQuery`, self-clamping paging
  (`PageSize <=0 or >100 → 20`), optional `StatusFilter`. Identity is never a parameter.
- **`GetProviderDisputesQueryHandler.cs`** — resolves the provider **server-side** from the trusted token
  (`_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo.ProviderProfileId`; `profileId <= 0` → empty, never fabricated),
  one grouped `ServiceRequestDbContext` query (offers subquery → disputes → `Join` SR header), no N+1. Follows the
  `GetProviderConversations` DbContext-projection idiom for provider composed reads. `OpenCount` is computed **globally**
  (independent of the page's status filter) so the dashboard row is accurate regardless of paging.
- **`Controller/V1/Jobs/ProviderJobsController.cs`** — added `[HttpGet("disputes")]` (route
  `api/v1/service-requests/provider/disputes`, `[Authorize]`), identity from the trusted context.

## MarineProvider BFF (new)

- **`IServiceRequestRemoteCall.cs`** — added the Refit passthrough `GetProviderDisputes(pageIndex, pageSize, status?)`.
- **`ServiceRequests/Query/GetProviderDisputesBff/{Query,QueryHandler}.cs`** — mirrors `GetProviderConversationsBff`:
  `ResolveAsync` first (populates the identity holder so the outgoing auth handler attaches the trusted-BFF assertion),
  throw if no profile link, then pass through the module response as-is. Envelope-correct, cost-free.
- **`Controllers/V1/DisputesController.cs`** — `GET /api/v1/provider/disputes?pageIndex&pageSize&status`,
  `[Authorize(Policy = ProviderActive)]` (Approved+Active profile). The client never sends a provider id.

## provider-web (new + wiring)

- **`features/disputes/api/disputesApi.ts`** — `getMine({pageIndex,pageSize,status})` → `httpClient.get('/disputes')` +
  `normalizeEnvelope`. `ProviderDispute` / `ProviderDisputesResponse` types. No provider id ever leaves the browser.
- **`features/disputes/hooks/useDisputes.ts`** — `useProviderDisputes()` (the list) and `useProviderOpenDisputeCount()`
  (dashboard; reads `openCount` off the same endpoint with `pageSize: 1`, `select`-ing to a number that soft-fails to 0).
- **`features/disputes/pages/DisputesPage.tsx`** — the destination surface: header, loading/error/empty states, and a
  list (SR code + title, localized reason + opened date + "opened by you", a status badge). Each row links to the SR
  detail (`paths.app.serviceRequests.detail`) — the working destination where the dispute surfaces.
- **`features/dashboard/pages/DashboardPage.tsx`** — added the previously-omitted **"disputes needing input"** attention
  row (Scale icon, danger tone), fed by `useProviderOpenDisputeCount()`, hidden at 0 like the other rows, linking to
  `/app/disputes`; folded the new query into `attentionLoading`. Removed the stale "intentionally omitted" comment.
- Wiring: `endpoints.disputes.list`, `paths.app.disputes`, `queryKeys.disputes.*`, `namespaces` (+`disputes`),
  `i18n.ts` (+`disputes` resources), `routes.tsx` (`/app/disputes` → `DisputesPage`).
- **i18n** — new `en/tr disputes.json` (parity **23/23**; status + reason labels, states) and `attention.disputes` added
  to `en/tr dashboard.json` (parity **31/31**). No dedicated sidebar nav item — the surface is reached from the
  dashboard row (kept nav unchanged; out of scope).

## Verify (on screen — localhost:3002, PROVIDER 2 AS / Cihan Çakır, tr)

Rebuilt + recreated `service-request-api`, `bff-marineprovider`, `bff-marineprovider-2` (two replicas). PROVIDER 2's
provider profile is **100011**; they won `SR-SEED-EMERGENCY-1` (SR **9011**).

- ✅ **Provider-scoped (the core correctness check).** With the system holding **3 disputes** (on SRs won by *other*
  providers 11011/11012/11013), PROVIDER 2's `GET /api/v1/provider/disputes` returned **200 with 0 items** — they see
  none of the others'. Identity is from the token/BFF assertion; **no provider id is sent from the browser** (the call
  carries no id param).
- ✅ **Hidden at 0.** Dashboard attention strip showed only the negative-balance row (no disputes row); the disputes
  page showed the empty state ("İtiraz yok").
- ✅ **Real count when open (seeded one open dispute on SR 9011 — PendingProviderResponse — then removed it).** The
  dashboard row appeared: **"⚖ Girdi bekleyen 1 itiraz"** (danger tone). Clicking it navigated to `/app/disputes`,
  which listed **SR-SEED-EMERGENCY-1 · Acil: Dümen sistemi arızası — Çeşme**, "Kalite sorunu · 07.08.2026 tarihinde
  açıldı", badge **"Yanıtın bekleniyor"**. The row linked through to the SR detail (`/app/service-requests/9011`).
- ✅ **No cost fields** anywhere in the payload or UI (SR code/title/reason/status/date only).
- ✅ **tr rendered**; **en parity 23/23 (disputes) + 31/31 (dashboard)**. **No runtime console errors** on a clean load
  (the transient errors seen mid-session were Vite HMR reloads during editing + SignalR reconnects from the BFF
  container restart — none from this code).
- ✅ **tsc + eslint = 0** (provider-web); **both backend projects build 0 errors**.

Test data restored: the seeded dispute (`Id 999999`) was deleted — the DB is back to the original 3 disputes, none
belonging to PROVIDER 2.

## Scope

New (8): SR `GetProviderDisputesResponse` + `GetProviderDisputes` query/handler; BFF `GetProviderDisputesBff`
query/handler + `DisputesController`; FE `disputes` api/hooks/page + `disputes.json` ×2. Edited (9): SR
`ProviderJobsController`; BFF `IServiceRequestRemoteCall`; FE `endpoints.ts`, `paths.ts`, `queryKeys.ts`,
`namespaces.ts`, `i18n.ts`, `dashboard.json` ×2, `DashboardPage.tsx`, `routes.tsx`. **No economics math, no schema
change.** **Not committed.**
