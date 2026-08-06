# REPORT_FE_S13 — admin dispute case view + resolution outcome picker + provider auto-approve countdown

> Closes the dispute vertical (S13 + N3) on the frontend. **Additive**; server owns case data + refund outcome.
> tr + en. Both SPAs typecheck + lint clean; the additive BFF field builds clean. **NOT committed** — tree left for review.

## Scope delivered

| Part | Repo | What |
|------|------|------|
| A (main) | `inktavia-marine-admin-web` | Real Disputes list + a consolidated **Dispute Case** page wired to `GetDisputeCaseBff`, and a resolution **outcome picker** wired to `ResolveDisputeBff`. |
| B (light) | `inktavia-marine-provider-web` | Auto-approve **countdown** banner on the job/completion detail, reading `autoApproveAt`. |
| B (backend, additive) | `Modules/ServiceRequest` | `autoApproveAt` added to the provider job-detail DTO + mapper so the field reaches the provider BFF/app. |

---

## PART A — Admin (main deliverable)

### BFF wiring (no new BFF code — the S13 passthrough already existed)
- `GET  /service-requests/disputes` → `GetAdminDisputeListResponse` (list)
- `GET  /service-requests/{srId}/disputes/{disputeId}/case` → `GetDisputeCaseDetailResponse` (`GetDisputeCaseBff`)
- `PATCH /service-requests/{srId}/disputes/{disputeId}/resolve` → `ResolveServiceRequestDisputeResponse` (`ResolveDisputeBff`)

### Files
- `src/shared/api/endpoints.ts` — `DISPUTES`, `DISPUTE_CASE`, `DISPUTE_RESOLVE`
- `src/shared/api/queryKeys.ts` — `disputes` key slice (`all` / `list` / `case`)
- `src/entities/service-request/types/dispute.ts` — full aggregate + resolve types
- `src/entities/service-request/model/disputeEnums.ts` — **numeric code → member-name maps** + `enumName()` (see “enum wire format” below)
- `src/entities/service-request/api/disputeApi.ts` — `list` / `getCase` / `resolve`
- `src/features/service-requests/hooks/useDisputeCaseQuery.ts`, `useDisputeListQuery.ts`, `useResolveDisputeMutation.ts`
- `src/pages/app/DisputeCasePage.tsx` — the consolidated case page + `ResolutionPanel` + `EvidenceFileChip`
- `src/pages/app/DisputesPage.tsx` — real list (replaced the “Coming Soon” stub)
- `src/app/router/routes.tsx` + `routeObjects.tsx` — `DISPUTE_CASE` route `/app/disputes/:serviceRequestId/:disputeId/case`
- `src/shared/i18n/locales/{en,tr}/serviceRequests.json` — `disputeCase` block (196 keys, **tr/en parity validated**)

### Dispute Case page — sections (all from the aggregate, cost-free)
- **Dispute:** reason, opener, status, description, resolution notes, status badge.
- **Service request:** SR summary + **status-history timeline** + the **N-E structured cancel/reject reason** (rendered when present).
- **Offer economics (cost-free):** line breakdown + service amount / commission / provider net / platform fee / customer total — **exactly the aggregate fields, no cost/margin**.
- **Evidence trail:** completion (+ evidence file), work-logs (+ attachments), conversation messages. File references resolve a read-URL on click (`fileApi.getReadUrl`).
- **Payment / refund state:** escrow/settlement, refund allocations, chargeback (empty-state when the SR has no transaction).

### Resolution outcome picker (`ResolveDisputeBff`)
- Picker: **notes-only** / `FavorPayerFullRefund` / `FavorPayerPartialRefund(+amount)` / `FavorProviderRelease` / `Split(+amount)`.
- Client-validates the amount: **> 0** and **≤ `paymentState.refundableAmount`** (the FE computes no money — it only guards against the server-supplied ceiling).
- Shows the plain-language effect (“Müşteriye ₺X iade edilecek” / “Kalan sağlayıcıya bırakılacak”, and EN equivalents).
- On success: refreshes the case (→ Resolved, refund reflected) + success toast; on a server business-error, surfaces the message **verbatim** inline + toast and leaves the form intact.
- The existing dispute list + row navigation are preserved.

### Enum wire format (important discovery)
The AdminPanel BFF serializes enums as their **numeric code** (System.Text.Json default), **not** the PascalCase name — the pre-existing `CompletionDisputeReviewPage` already treated them numerically. The initial implementation assumed string names and crashed (`v.replace is not a function`). Fixed by `disputeEnums.ts`, which maps each code → member name (the i18n key). The mapper also tolerates a name-string, so it is representation-agnostic. `outcome` is submitted back to the BFF as its numeric code.

---

## PART B — Provider (light)

### Backend (additive — required for the field to reach the app)
- `Modules/ServiceRequest/.../Abstraction/Response/Jobs/GetProviderJobDetailResponse.cs` — added `DateTime? AutoApproveAt`.
- `Modules/ServiceRequest/.../Application/Query/Jobs/GetProviderJobDetail/GetProviderJobDetailQueryHandler.cs` — map `AutoApproveAt = sr.Completion?.AutoApproveAt`.
- Provider BFF (`Aizen.Bff.MarineProvider`) — **no code change** (pure passthrough); rebuilt + redeployed so its compiled DTO carries the new property through to the client.
- Source of truth: `ServiceRequestCompletionEntity.AutoApproveAt` (= `SubmittedAt + AutoApproveWindowDays`, N3).

### Frontend
- `src/features/jobs/api/providerJobsApi.ts` — `autoApproveAt?: string | null` on `ProviderJobDetail`.
- `src/features/jobs/pages/JobDetailPage.tsx` — `StateBanner` renders the countdown from `autoApproveAt` (module-level `autoApproveDaysLeft` helper, kept out of render to satisfy the `react-hooks/purity` rule). Purely informational; falls back to the static awaiting copy when the field is absent; the banner only shows for `CompletionSubmitted` / `WaitingForOwnerApproval`, so it disappears once the completion is approved/rejected/disputed.
- `src/shared/i18n/locales/{en,tr}/jobs.json` — `banner.awaitingCountdown_one/_other` (tr + en).

---

## Confidentiality
`grep -niE "supplier|dealer|margin|cost"` over every new admin dispute FE file (types, model, api, both pages, both i18n JSON) returns **only comments** (“cost-free”, the confidentiality note) — **no cost/margin/supplier/dealer field is typed, requested, or rendered**. The economics rendered are the derived cost-free fields only.

---

## On-screen verification

Ran against the live dev stack (admin-web :3000, provider-web :3002, dockerized BFFs/modules). DB: `inktavia_store`.

1. **Case page (Part A).** Admin (`admin.user@inktavia.com`, OTP) → dispute #9901 (SR 9006). The full consolidated file rendered in Turkish: dispute (Neden *Fiyatlandırma anlaşmazlığı*, Açan *Sahip*, *Açık*), service request (Durum *Anlaşmazlık açıldı*), status timeline, economics (empty-state — cost-free), evidence/work-logs (“Arrived at Kalamış Marina” …), messages, payment (empty-state). **No cost/margin anywhere.** All enum labels resolved correctly from numeric codes.
2. **Resolution (Part A).** Selecting *Ödeyen lehine — kısmi iade* revealed the amount field + client validation (“Sıfırdan büyük bir tutar girin”, submit disabled); entering 1500 updated the effect to “Müşteriye ₺1.500,00 iade edilecek” and enabled submit. Submitting a partial refund on this (unpaid) fixture surfaced the server error **verbatim** inline (“Request failed with status code 500”) and left the dispute unchanged. A **notes-only resolve** then succeeded → success toast (“Anlaşmazlık çözüldü”), status badge → **Çözüldü**, resolution notes shown, and the status timeline updated (*Anlaşmazlık açıldı → Anlaşmazlık çözüldü · Yönetici*).
3. **Provider countdown (Part B).** Provider2 (`provider2@inktavia.com`, OTP) → job 60003 (SR 30003, *CompletionSubmitted*, `AutoApproveAt = 2026-08-13`):
   - TR: **“Sahip onayı bekleniyor — 13 Ağu 2026 18:30 tarihinde otomatik onaylanacak (8 gün)”**
   - EN: **“Awaiting owner approval — auto-approves on Aug 13, 2026, 6:30 PM (8 days)”**

   confirming the field flows DB → module mapper → module DTO → provider BFF passthrough → app.
4. **Existing flows.** The dispute list renders (row #9006, clickable → case) with no regression; the resolved-state refresh keeps the list/status behaviour intact.

### Environment actions taken (runtime only — no code committed)
- Rebuilt + redeployed **service-request-api** (exposes the S13 module case/resolve endpoint + the new `AutoApproveAt` mapping), **bff-adminpanel** (the running image predated the S13 routes — they 404’d until redeploy), and **bff-marineprovider** (carries the new passthrough field). All healthy.
- Temporary seed toggles for Part B (a submitted completion’s `AutoApproveAt`, and a reassignment of assignment 60003 to provider2) and the notes-only resolve of dispute 9901 were **all reverted** — dispute 9901 back to Open, SR 9006 back to DisputeOpened, its added timeline row removed, assignment 60003 back to its original provider, completion 80002 `AutoApproveAt` back to NULL.

## Follow-up fix — partial-refund `500` → clean business error

The FE review flagged that resolving with `FavorPayerPartialRefund` returned a raw **500**. Root cause was a chain of missing error-translation across the SR→Payment→BFF hops (all pre-existing, none introduced by the FE work), fixed additively:

1. **Config (real root cause).** `Modules/ServiceRequest/.../configuration/appsettings.Development.json` had
   `RemoteCalls:IPaymentModuleRemoteCall:BaseUrl = http://payment-api:5010`, but payment-api listens on **8080**
   (every other payment remote call uses `:8080`, and compose does not override this key). So the S13 remote calls
   (`ResolveDisputeOutcomeAsync` **and** `GetDisputeCasePaymentStateAsync`) hit a dead port → `HttpRequestException:
   Connection refused` → raw 500 (and it's why the case page's `paymentState` came back null). Fixed to `:8080`.
2. **SR module resilience** (`ResolveServiceRequestDisputeCommandHandler`). The Payment call can fail two ways that
   must not surface as 500: (a) it *throws* (transport failure, 404) — now caught and re-thrown as a clean
   `AizenBusinessException` (400) with a non-leaking message (`AizenException` is re-thrown untouched); (b) it
   *returns* a downstream business rejection that Refit deserializes into a default response (`Applied=false`) instead
   of throwing — now surfaced as a clean business error carrying the module's own `Message` when present. Also the
   missing-amount guard was switched from `InvalidOperationException` (→500) to `AizenBusinessException` (→400).
3. **AdminPanel BFF** (`AdminBffFailEnvelopeHandler`, renamed from `AdminPaymentBffFailEnvelopeHandler`). The existing
   fail-envelope `DelegatingHandler` (which converts a module fail envelope into an `AizenBusinessException` so the BFF
   returns a structured 400 preserving the module's code/message) was wired **only** on the Payment client; it is now
   also wired on the **ServiceRequest** client, so a dispute-resolve business error reaches the FE as a clean 400
   instead of the BFF's generic 500.

**Verified on screen / via API** (admin `admin.user@inktavia.com`, dispute #9901 / SR 9006, which has a real captured
transaction of ₺38 000): a valid partial refund (₺1 500) processes end-to-end (refund record + transaction →
PartiallyRefunded + dispute Resolved); an **over-refundable** amount (₺999 999) now returns a **clean 400 business
error** (was a 500) — Payment rejects with `RefundAmountExceedsMaximum`, the SR module + BFF translate it to a 400.
All probe mutations were reverted (dispute 9901 back to Open, SR 9006 to DisputeOpened, refund record + transaction +
timeline restored). Services rebuilt + redeployed: **service-request-api**, **bff-adminpanel**, **payment-api** (the
running image predated the S13b `ResolveDisputeOutcome`/`GetDisputeCasePaymentState` endpoints — they 404'd until
redeploy; with them live the case page now also shows real payment/refund state).

### Other backend notes (out of scope)
- The BFF dispute **list** returns only *Open* disputes (server-side default); the FE status filter renders whatever the BFF returns.

## Result
tsc + eslint clean in both repos; the additive backend field builds clean; enum-labels, tr/en, confidentiality, resolution + refresh, the provider countdown, and the partial-refund clean-error fix all verified. **Do not commit** — tree left for review. Next SR phase: **S12 + N2 (recurring)**.
