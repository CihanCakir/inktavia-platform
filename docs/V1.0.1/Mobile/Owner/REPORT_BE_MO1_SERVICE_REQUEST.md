# REPORT — BE_MO1: owner service-request create / list / detail (mobile)

> Executes `BE_MO1_SERVICE_REQUEST_CREATE_LIST_DETAIL.md`. First phase of the Owner App Economics track.
> `Bff/src/Marine.Participant.Mobile` (BFF) + `inktavia-marine-mobile` (Expo). **Cost-free** (no economics),
> no external gate. Mirrors the mobile Vessel feature. **NOT committed.**

---

## Headline decision (Phase-0 blocker, user-approved)

The module owner endpoints resolve the owner from the **asserted `UserInfo.UserId`** — set by the mobile BFF's
Keycloak service-token + `X-Aizen-Bff-Assertion` path — **except `GET /api/v1/service-requests/my`**, whose
controller read `long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))`. Under the assertion that claim is
the Keycloak **service-account** subject (a GUID), so `long.Parse` threw → the list would 500. Nothing maps the
asserted `X-Aizen-User-Id` into that claim (the claims-transformation injects only roles).

**Root cause (reusable trap):** on the BFF-assertion path, owner endpoints must read
`_info.UserInfoAccessor.UserInfo.UserId`, **not** `ClaimTypes.NameIdentifier` (`CurrentUserId`). The create /
update / cancel / publish / attachment handlers already do; only the `my` controller line didn't.

**Fix (user-approved, minimal, backward-compatible):** `ServiceRequestController.CurrentUserId` now prefers
`_info.UserInfoAccessor.UserInfo.UserId` (asserted) and **falls back** to the `NameIdentifier` claim for a direct
Identity-JWT caller. Any existing caller keeps working; the BFF-assertion caller now resolves. **Audit:** the other
owner actions were verified to already resolve from `UserInfo.UserId` — no further module change. This is the only
byte touched outside `Marine.Participant.Mobile` + the Expo client (plus config wiring).

---

## BE — mobile BFF `ServiceRequest` feature

**Remote client** `Common/RemoteClients/IServiceRequestRemoteCall.cs` (Refit, typed, envelope-correct) → the owner
endpoints, verbs mirrored exactly: `Create` POST, `Publish` **PATCH** `{id}/publish`, `GetMy` GET `my`, `GetDetail`
GET `{id}`, `Update` PUT `{id}`, `Cancel` **PATCH** `{id}/cancel`, `AddAttachment` POST `{id}/attachments`.
Registered in `DependencyInjection` via the existing `CreateRemoteCall`/`CreateHttpClient` pattern (shared auth
delegating handler + service-token). Base URL from `RemoteCalls:IServiceRequestRemoteCall:BaseUrl`.

**Feature slices** (`ServiceRequest/Command|Query/<Name>Bff`, `Mobile…` naming), each resolving identity via
`IParticipantProfileResolver` (sets the holder → the assertion carries `X-Aizen-User-Id`) and mapping module DTOs →
mobile DTOs (`MobileServiceRequestMapper`):
- **CreateMobileServiceRequest** — create Draft → recover the committed id by the unique `RequestCode` from `GetMy`
  (the module builds the create response pre-commit, so its `Id` is 0, same as the Vessel create) → attach any
  pre-uploaded files (best-effort) → publish if `Publish` (best-effort) → return the assembled detail.
- **GetMobileMyServiceRequests** — paged `MobileServiceRequestListDto`; empty list on any failure (never a 500).
- **GetMobileServiceRequestDetail** — request + status timeline + attachments (with resolved presigned read URLs);
  **cost-free** (offers/economics dropped).
- **UpdateMobileServiceRequest**, **CancelMobileServiceRequest** (N-E structured `ReasonCode` + optional note),
  **AddMobileServiceRequestAttachment** (FileStorage client-side-presigned pattern, as Vessel documents).
- Mobile DTOs: `Contracts/ServiceRequest/MobileServiceRequestDtos.cs` (list/detail/timeline/attachment + create/
  update/cancel/attachment requests). No economics fields.

**Controller** `Controllers/V1/ServiceRequestsController.cs` at `api/v1/mobile/service-requests`
(`[Authorize(Policy = ParticipantAuthenticated)]`): POST create (+publish), GET my, GET `{id}`, PUT `{id}`,
POST `{id}/cancel`, POST `{id}/attachments`. Mirrors the mobile Vessel controller.

### Identity-from-token + ownership (security)

- The owner id is **never** taken from the body/query — it is resolved server-side from the validated token
  (Keycloak `sub` → Identity by-subject → numeric UserId) and asserted to the module via the `ModuleAssertionSecret`
  headers; the inbound JWT is not forwarded.
- **Owner-spoof-via-body is impossible** — no BFF DTO carries an owner id; the module stamps `OwnerUserId` from the
  asserted `UserInfo.UserId`.
- **BFF-side ownership gate** — the module's detail/update/cancel/attachment handlers fetch by id and do **not**
  check `OwnerUserId == caller` (they use the caller only for history/realtime). So every by-id mobile action calls
  `MobileServiceRequestMapper.EnsureOwnedAsync` first: fetch the detail, require `detail.Request.OwnerUserId ==
  resolved owner id`, else a clean not-found — a caller can neither read nor modify another owner's request. (This
  mirrors the Vessel BFF's owner gate on detail.)

### Config wiring (values from env/k8s, not the repo)

- `appsettings.json` → `RemoteCalls:IServiceRequestRemoteCall:BaseUrl` (`__FROM_ENV__`).
- `docker-compose.yaml` → mobile BFF `RemoteCalls__IServiceRequestRemoteCall__BaseUrl: http://service-request-api:8080`
  + `depends_on: service-request-api`; and the ServiceRequest module gains
  `BffAssertion__AllowedClientIds__2: marine-mobile-bff` so it honors the mobile BFF's assertion (as vessel-api /
  identity-api already do).

**Build:** ServiceRequest module, mobile BFF, and the **full solution** all build with **0 errors**.

---

## FE — Expo client (`inktavia-marine-mobile`)

**Data layer** (new): `features/service-requests/api/serviceRequestsApi.ts` (types + fns via `httpClient` +
`ENDPOINTS.SERVICES`) and `.../useServiceRequests.ts` (React Query hooks: `useMyServiceRequests`,
`useServiceRequestDetail`, `useCreateServiceRequest`, `useCancelServiceRequest`). `ENDPOINTS.SERVICES` now points at
the real BFF (`/api/v1/mobile/service-requests` BASE/BY_ID/CANCEL/ATTACHMENTS); offers/chat/complete/dispute stay on
the old paths (coming-soon). Shared display helpers in `features/service-requests/utils/display.ts`.

**Envelope mapping (acceptance criterion):** every read/write flows through the interceptor's `normalizeEnvelope`,
which adapts the fixed `AizenApiResponse<T>` (`{header,body}`) → the client `ApiResponse<T>` (`{data,success,…}`);
the paged list maps to `MobileServiceRequestListDto` = `{ items, totalCount, pageIndex, pageSize }`.

**Screens** (routed via `ServicesNavigator`; `features/services/*`):
- **List (`ServicesHomeScreen`)** — driven by `useMyServiceRequests()`; **Loading / EmptyState(+create CTA) /
  ErrorState**. Removed the hardcoded `MOCK_REQUESTS` / `VESSEL_FILTERS` / `'Sea Serenity'` / `'Azure Dream'`; the
  filter chips are `t('services.allVessels')` + the owner's **real** vessels (`useVessels()`), filtered client-side
  by `vesselId`. Card mapping: category label via `useReferenceLookup('SERVICE_PROVIDER_CATEGORY')`, vesselId→name
  map, status→card status, priority→urgency, `relativeTime(createdAt)`.
- **Detail (`ServiceRequestDetailScreen`)** — `useServiceRequestDetail(id)`; loading/error/not-found. Renders title/
  status/vessel/category/description, the **status timeline** from `detail.timeline`, and **attachments** from
  `detail.attachments` (tappable `downloadUrl`). A **Cancel** action (only when `detail.canCancel`) opens a bottom-
  sheet **N-E reason picker** (7 `ServiceRequestCancelReason` codes, i18n labels) + optional note →
  `useCancelServiceRequest`. Offers/chat/completion are a **coming-soon** stub (no economics surfaced).
- **Create (`CreateServiceRequestScreen`, 3-step wizard)** — Step 1 vessel from `useVessels()` + category from the
  reference lookup (stores the code); Step 3 Submit → `useCreateServiceRequest({…, publish:true})`, Save Draft →
  `publish:false`; priority from urgency; on success navigates to the created detail.

**Mock parity:** `core/mock/handlers/serviceRequests.handlers.ts` serves the new routes (GET list, GET `{id}`, POST
create, POST `{id}/cancel`) with realistic camelCase mobile DTOs, so `EXPO_PUBLIC_MOCK_MODE=true` renders through
the same `httpClient` path; the coming-soon offers/chat handlers are untouched.

**i18n:** `services.*` keys added to `en.json` + `tr.json` (**35/35 parity**). **tsc:** `npx tsc --noEmit` → **exit 0**
(the repo has no ESLint configured — verified — so tsc is the type/lint bar).

### Note — subagent over-reach, reverted

The FE screen-wiring subagent additionally introduced an unrelated `PressableScale`/haptics refactor (a new
component, an `expo-haptics` dependency, and edits to ~11 shared UI files: Button, IconButton, several cards,
ProfileScreen, tokens, CommandHubNav). That is out of scope for BE_MO1 and was **fully reverted** (`git checkout`
of the 13 files + removal of `PressableScale.tsx` and the agent's `plans/`/`docs/UI/` artifacts). The BE_MO1 SR
slice does not depend on it; tsc is still 0 after the revert. The final FE diff is exactly: `endpoints.ts`,
`serviceRequests.handlers.ts`, `en/tr.json`, the three `features/services` screens, and the new
`features/service-requests/{api,utils}`.

---

## QA

- **Programmatically verified:** BE builds (module + BFF + full solution, 0 errors); FE `tsc` clean; demo constants
  removed from the real data path; envelope mapping in place; mock parity for `EXPO_PUBLIC_MOCK_MODE`; identity
  resolved server-side with no owner-id in any request DTO (spoof-proof) + BFF ownership gate on every by-id action.
- **On-device E2E (create → list → detail → cancel-with-reason) pending the running stack** (keycloak + identity /
  reference-data / vessel / file-storage / service-request APIs + the mobile BFF + a device/simulator). All four
  steps are exercised in `EXPO_PUBLIC_MOCK_MODE` against the mobile-shaped mock handlers; the live device pass is the
  remaining manual step per the M3/M4 cadence.

**Next: MO2** — offers inbox + cost-free economics breakdown.

**Do NOT commit.**
