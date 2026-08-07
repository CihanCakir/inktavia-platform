# BE_MO1 — owner service-request create / list / detail (mobile)

> **Repos:** `addesso-project` (`Bff/src/Marine.Participant.Mobile`) + `inktavia-marine-mobile` (Expo). First phase of the
> **Owner App Economics track** (`OWNER_APP_ECONOMICS_ROADMAP.md`). Gives the owner the ability to **create, list, and open**
> their service requests — the foundation the whole owner economics flow (offers → checkout → completion → dispute) builds
> on. **No economics yet** (offers/pricing/checkout = MO2/MO3) → this phase is **cost-free** and has **no external gate**.
> Additive; mirror the existing mobile Vessel feature; follow the `MOBILE_ROADMAP` invariants. BE + FE in one slice, then a
> `REPORT_BE_MO1_*.md` (mobile per-slice cadence).

## Baseline (investigated)
- **Mobile BFF** (`Marine.Participant.Mobile`) already has Auth/Profile/Vessel built in the MarineProvider convention
  (per-command `Command/<Name>Bff/…` subfolders, `Mobile<Verb><Noun>` naming, mappers). RemoteClients present:
  `IIdentity`, `IReferenceData`, `IVessel`, `IFileStorage` — **no `IServiceRequest` yet.** The **Vessel feature is the
  pattern to mirror** (`Vessel/Command/CreateMobileVessel/…`, `Vessel/MobileVessel*Mapper.cs`, mobile controller).
- **ServiceRequest module owner endpoints** exist at **`api/v1/service-requests`** (owner CRUD/lifecycle): `POST` (create),
  `POST publish`, `PUT {id}` (update), `GET {id}` (detail), **`GET my`** (the owner's list), `POST {id}/cancel` (carries the
  **N-E `ServiceRequestCancelReason`** + note), `POST {id}/attachments`. Commands/queries:
  `CreateServiceRequest`/`PublishServiceRequest`/`UpdateServiceRequest`/`CancelServiceRequest` + detail/my-list queries.
- Client screens exist as static: `features/services/*` (3 screens, main entry) + `features/service-requests/*` (deep
  flow), both routed via `ServicesNavigator`; `VESSEL_FILTERS`/`'Sea Serenity'` demo constants to remove (per
  `MOBILE_STATIC_TO_API_ROADMAP`).

## BE — mobile BFF `ServiceRequest` feature
1. **`IServiceRequestRemoteCall`** in `Common/RemoteClients` (Refit, typed, envelope-correct `AizenApiResponse<T>`) pointing
   at the **owner** SR endpoints (`api/v1/service-requests`: create, publish, `my`, `{id}`, update, `{id}/cancel`,
   `{id}/attachments`). Mirror the existing mobile client structure; register it + wire the `X-Aizen-Bff-Assertion`
   delegating handler + audience mapper for the ServiceRequest module (as Vessel does).
2. **Feature slices** (`ServiceRequest/Command|Query/<Name>Bff/…`, `Mobile…` naming):
   - `CreateMobileServiceRequest` — the owner's create (vessel + category/type + title/description + requested dates +
     location + attachments). If the module flow is create-draft-then-publish, expose create + a `PublishMobileServiceRequest`
     (or a single create-and-publish per the module's contract — match it).
   - `GetMobileMyServiceRequests` — paged `PagedResponse` of the owner's requests (status, category, vessel, created date,
     summary) for the list screen.
   - `GetMobileServiceRequestDetail` — the request + its **status timeline** + attachments (cost-free; offers/economics are
     MO2). 
   - `UpdateMobileServiceRequest`, `CancelMobileServiceRequest` (with the **N-E structured reason** + optional note),
     `AddMobileServiceRequestAttachment` (via the FileStorage upload pattern already used by Vessel documents/media).
   - Mobile-shaped DTOs + mappers (module DTO → mobile DTO), consistent with the client's `ApiResponse<T>` expectations.
3. **Controller** `api/v1/mobile/service-requests` (mirror the mobile Vessel controller): POST create (+ publish), GET my,
   GET `{id}`, PUT `{id}`, POST `{id}/cancel`, POST `{id}/attachments`. `[Authorize]` participant policy.
4. **Identity from the token only:** `ownerUserId`/`participantProfileId` resolved server-side from validated claims
   (`sub`/`participant_profile_id`) — **never** from the request body/query; the module call carries the identity via the
   BffAssertion headers (`ModuleAssertionSecret`), the inbound JWT is not forwarded. Ownership is enforced module-side (an
   owner only sees/edits their own requests).

## FE — wire the SR create/list/detail screens (`inktavia-marine-mobile`)
- Add `features/service-requests/api/` (or reuse the existing `api/` if present); a data layer + hooks hitting the new BFF
  endpoints via `normalizeEnvelope`.
- **List:** the owner's requests from `GET my` with **loading / empty / error** states; remove the `VESSEL_FILTERS` /
  `'Sea Serenity'` / `'Azure Dream'` hardcoded demo (drive filters from the owner's real vessels).
- **Create:** the 3-step create flow (basic / details / attachments) → `POST` (+ publish); vessel + category pickers fed by
  the real vessel list + the M3 reference lookups.
- **Detail:** the request + status timeline + attachments from `GET {id}`; a **Cancel** action wired to `POST {id}/cancel`
  with the **N-E reason picker** (structured `ServiceRequestCancelReason` + optional note). (Offers/chat/complete/dispute are
  later MO phases — leave those sections as stubs/coming-soon for now.)
- **Mock parity:** keep the `EXPO_PUBLIC_MOCK_MODE` mock branch realistic; real BFF when the flag is off.

## Guardrails / QA (mobile invariants)
- Envelope mapping (`AizenApiResponse<T>` → client `ApiResponse<T>`/`PagedResponse<T>`) is an acceptance criterion. Additive
  — only `Bff/src/Marine.Participant.Mobile/` + the Expo client; every other BFF/module/panel stays byte-for-byte clean.
  Secrets from env; nothing logged. Cost-free — no economics fields surfaced.
- BE builds 0 errors; identity resolved from claims (verify a request can't spoof another owner via body). FE tsc + lint
  clean; loading/empty/error on every data screen; mock parity holds. On-device QA per the M3/M4 cadence (create a request,
  see it in the list, open detail, cancel with a reason).

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO1_SERVICE_REQUEST.md`: the SR mobile client + feature slices, the controller, the
identity-from-token enforcement, the FE wiring (list/create/detail + cancel-with-reason, demo constants removed), and the
QA. Then **MO2** (offers inbox + cost-free economics breakdown).
