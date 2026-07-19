# JD-1 — Backend: Job detail aggregate endpoint

Unblocks the Job Workspace (SCREEN_47). One access-scoped aggregate for a single assignment: the assignment
(status/dates/notes) + its ServiceRequest (title/code/description/work scope/attachments/location) + the **accepted
offer** (line items + totals) + a durable **timeline**. Provider-scoped; the SR/offer/assignment are all in the
ServiceRequest module, so it assembles **in-module** (no cross-module call) — only the vessel name is BFF-enriched.

## Verified in source (2026-07-17)
- `ServiceRequestAssignmentEntity`: `ServiceRequestId, ServiceRequestOfferId, ProviderProfileId, ProviderUserId,
  Status, ScheduledStart/EndDate, ActualStart/EndDate, ProviderNotes, RejectionReason, CancellationReason`.
- Offer: `IServiceRequestOfferRepository.GetByIdAsync(offerId)` → the accepted offer entity (load **with items**).
- A provider SR-detail aggregate already exists (`GetProviderServiceRequestDetailQuery` →
  `ProviderServiceRequestDetailDto`: title, requestCode, description, work scope, attachments, location, vessel spec
  fields, timeline). **Reuse its assembly** for the SR part — do not duplicate work-scope/location/timeline logic.
- JB-6 aligned the jobs list `status` to `sr.Status`. The detail's header/stepper use the **SR lifecycle**
  (`ServiceRequestStatus`: Assigned/Scheduled/InProgress/WaitingForOwnerApproval/WaitingForMaterial/Paused/
  CompletionSubmitted/Completed), so expose `status = sr.Status`. Optionally also `assignmentStatus` (the sub-state).

## Work

### 1. Module — `GetProviderJobDetailQuery(long assignmentId)` + handler
- Resolve the provider from the assertion (`_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId`).
- Load the assignment by id. **Access check:** if it's null OR `assignment.ProviderProfileId != resolvedProfileId`
  → throw the same "not found" the detail uses (never reveal another provider's job). Fail closed.
- Assemble `ProviderJobDetailDto`:
  - **Job/assignment:** `assignmentId`, `serviceRequestId`, `serviceRequestOfferId`, `status = sr.Status.ToString()`
    (SR lifecycle — for the stepper), `assignmentStatus = assignment.Status.ToString()` (sub-state),
    `scheduledStart/End`, `actualStart/End`, `providerNotes`.
  - **ServiceRequest:** reuse the existing detail assembly (`ProviderServiceRequestDetailDto` or its parts): `title`,
    `requestCode`, `description`, **work scope items**, **attachments** (metadata only — no URLs; the SPA mints them
    per-click via the existing 14c read-url), `location` (approx lat/lng, marina, city — snapped), `vesselId`
    (+ existing spec fields).
  - **Accepted offer:** load `GetByIdAsync(assignment.ServiceRequestOfferId)` **with items**; project the line items
    (`itemType, title, description, quantity, unitCode, unitPrice, discountType, discountValue, taxRate, lineSubtotal,
    taxAmount, discountAmount, lineTotal`) + `subtotal, discountTotal, taxTotal, grandTotal, currencyCode` +
    commercial notes (paymentTermsNote, warrantyNote). Read-only — never editable from the job.
  - **Timeline:** durable events from SR **status history** + lifecycle **system messages** (OFFER_ACCEPTED,
    JOB_STARTED, JOB_COMPLETED, CONVERSATION_CLOSED, plus status transitions) → `{ code/label, timestampUtc,
    actorRole }`. Only persisted events — no ephemeral SignalR entries.
- One efficient read (in-module joins across Assignments/ServiceRequests/Offers/OfferItems). No cross-module call.

### 2. BFF — `GET /api/v1/provider/jobs/{assignmentId:long}` (+ passthrough + vessel enrich)
- `GetProviderJobDetailBffQuery(assignmentId)` → module call, then **bulk-enrich `vesselName`** (one `GetSummaries`
  call, the JB-1 pattern; vessel specs Beam/Draft arrive with JD-4 later). Provider-scoped (`ProviderActive`),
  identity via the assertion. Response = `ProviderJobDetailDto` (+ vesselName).
- The route is a numeric `{assignmentId:long}` — no collision with `/jobs/summary|workload|action-required` (literals).

## Constraints
- **Access-checked before returning** — the caller must own the assignment; otherwise "not found". No client-sent
  profile id.
- **No customer identity** anywhere — role labels only ("Tekne Sahibi"). No budget — only the provider's own
  accepted-offer total. Attachments stay metadata-only (URLs minted per-click via 14c, access-scoped).
- In-module joins (assignment+SR+offer same module); vessel name is the only BFF enrichment. Money `decimal`. Dates
  UTC/Postgres-safe. Status/enum values stay codes (the SPA localizes). One read, no N+1.

## Acceptance — observed
- `GET /provider/jobs/91001` (the seeded job) → aggregate: `status` = current SR status (`Assigned` or `InProgress`),
  scheduled/actual dates, `title` "Acil: Dümen sistemi arızası — Çeşme", `requestCode` "SR-SEED-EMERGENCY-1",
  `vesselName` "Aegean Wind", work scope, location, the **accepted offer** line items + grandTotal (₺), and the
  timeline (OFFER_ACCEPTED, JOB_STARTED if started…).
- A `assignmentId` owned by **another** provider → "not found". A non-existent id → "not found".
- No customer identity; attachments carry no URLs; one vessel bulk call.

## Report
Append to `REPORT_BACKEND.md` ("JD-1"): the aggregate for job 91001 (assignment + SR + accepted offer + timeline),
the cross-provider "not found" rejection, and confirmation of one vessel bulk call + no PII. Unfinished is **not
done**.

## Frontend (I will build after this lands)
FE-A (header + lifecycle stepper + state-aware primary action) + FE-B (work scope + read-only accepted-offer table).
Sidebar quick-info/map + embedded messages, work logs, evidence, completion follow in later phases (JD-2..JD-5).
