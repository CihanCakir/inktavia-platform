# İş Detayı / Job Workspace (SCREEN_47) — Backend + Frontend Roadmap

The approved design is a rich **operational workspace** (header + lifecycle stepper + accepted-offer table + work
logs + evidence + activity timeline + embedded messages + completion flow + sticky sidebar with map). This roadmap
phases it, grounded in what exists vs what must be built, per our architecture (modular monolith, provider-scoped via
the BFF assertion, module-boundary rule, no customer PII).

## What EXISTS today (reuse — don't rebuild)
- `ProviderJobDto` (assignment status/dates/notes) + **JB-1 enrichment** (title, requestCode, vesselName).
- SR data in-module: title, description, **work scope items**, **attachments** (14c signed read-url), approx location.
- **Accepted offer**: offer entity + line items + totals (from the offer domain, 10b–10d).
- **Work logs**: `AddServiceRequestWorkLog` / `AddWorkLogEntry` commands, `GetServiceRequestWorkLogs` query, a seed —
  backed in the module; **not exposed to the provider BFF** yet.
- **Provider actions (commands exist)**: `StartServiceRequestAssignment` (start), `SubmitServiceRequestCompletion`
  (submit completion). **Not exposed to the provider BFF** yet.
- **Messages**: #18/#20 gated thread + composer (text/image/location) + lifecycle pills — reuse embedded.
- **Activity timeline** source: SR status history + lifecycle system messages (OFFER_ACCEPTED/JOB_STARTED/…).
- **Map**: MapLibre + OpenFreeMap `LocationMap` (built for Messages) — reuse.
- **File upload**: signed-URL flow (`/files/upload-session` → PUT → complete) — reuse for evidence.

## What is MISSING / different (must build or treat carefully)
| Design element | Reality | Action |
|---|---|---|
| **Job detail aggregate** `GET /jobs/{assignmentId}` | does not exist | build — **JD-1** |
| Start / Submit-completion actions | commands exist, **not on BFF** | build — **JD-2** |
| Work logs (list + add) on provider | module has it, **not on BFF** | build — **JD-3** |
| Vessel specs "En (Beam) / Draft" | summary has only Length + Year | widen — **JD-4** |
| Evidence / job / completion attachments | SR attachments only (14c) | extend read-url + upload — **JD-5** |
| **Pause / Resume / Schedule / WaitingForMaterial** actions | **no commands** | **do NOT show as functional** (state read-only) |
| Edit provider notes on the job | **no assignment-notes update command** | **read-only** (or a tiny command later) |
| Owner name / customer PII | forbidden | show role label "Tekne Sahibi" only |
| Rating "Müşteri Puanı 4.8", revenue/invoice | no Review/Payment contract active | **omit / Post-MVP** (do not fake) |

---

## Backend roadmap (provider-scoped; access-checked = provider owns this assignment)

### JD-1 — Job detail aggregate endpoint  ⭐ (biggest, unblocks the page)
`GET /provider/jobs/{assignmentId}` → one aggregate, access-scoped (reject a job not owned by the caller → "not
found"). Assemble **in the module** (assignment + SR + offer are all in ServiceRequest — in-module joins, no
cross-module call) except vessel name/specs (BFF bulk-enrich, like JB-1):
- Assignment: status, scheduled/actual dates, providerNotes, assignment ref.
- ServiceRequest: title, requestCode, description, **work scope items**, category, location (approx), vesselId.
- **Accepted offer**: line items (type/title/desc/qty/unit/unitPrice/discount/tax/lineTotal) + subtotal/discount/
  tax/grandTotal + currency + commercial notes.
- **Timeline**: durable events from SR status history + lifecycle system messages (assigned/scheduled/started/
  completion-submitted/completed) with timestamps.
- BFF enriches `vesselName` (+ specs from JD-4) via the bulk vessel call. Never customer identity.

### JD-2 — Provider action endpoints (expose existing commands)
- `POST /provider/jobs/{assignmentId}/start` → `StartServiceRequestAssignment` (→ InProgress + JOB_STARTED, already
  wired 20e). Guard: only from Assigned/Scheduled.
- `POST /provider/jobs/{assignmentId}/complete` → `SubmitServiceRequestCompletion` (→ CompletionSubmitted +
  JOB_COMPLETED path on owner approval). Backend stays authoritative on preconditions (e.g. InProgress, ≥1 work log
  if required). Return the new state.
- **Do NOT** add Pause/Resume/Schedule/Material actions — no commands exist. Those states, if reached, render
  read-only.

### JD-3 — Work logs (provider BFF)
- `GET /provider/jobs/{assignmentId}/work-logs` → `GetServiceRequestWorkLogs` (provider-scoped to the job's SR).
- `POST /provider/jobs/{assignmentId}/work-logs` → `AddServiceRequestWorkLog` (date, description, duration, optional
  attachments). Confirm the exact command shape and reuse it; validate in the module.

### JD-4 — Vessel spec widening (Beam / Draft)
Add `BeamValue/BeamUnitCode`, `DraftValue/DraftUnitCode` to the vessel summary DTO + query (the spec already has
`BeamValue`, `DraftValue`) so the "Gemi Özellikleri" card can show Boy/En/Draft. Bump the `vessel:summary:v*` cache
key. (Cheap — same pattern as the Year/Material widening.)

### JD-5 — Evidence attachments (read-url + upload authorization)
- Reuse the widened 14c access check so **work-log / completion / job** attachment fileIds resolve to signed read
  URLs (authorize: provider owns the job AND fileId belongs to a work-log/completion of that SR). Extend the access
  check's "is this fileId on this request" to include work-log/completion attachments.
- Upload uses the existing signed-URL flow; attach fileId to a work log / completion. No object key/bucket to the SPA.

---

## Frontend roadmap (build against JD-1's contract; reuse existing components)

- **FE-A — Shell + header + stepper.** Breadcrumb, title/code/vessel/location subline, status badge, **lifecycle
  stepper** (Atandı→Planlandı→Devam Ediyor→Tamamlama Gönderildi→Onay Bekliyor→Tamamlandı, + Malzeme/Duraklatıldı as
  blocking states), **state-aware primary action** (İşi Başlat / Tamamlamayı Gönder / passive). Works with fallback
  `ProviderJobDto` before JD-1.
- **FE-B — Main: Work Scope + Accepted Offer table** (JD-1). Icon work-scope list; read-only offer table (Kalem/Tür/
  Miktar/Birim Fiyat/KDV/Toplam + Genel Toplam ₺). Reuse the detail page's work-scope + the offer-builder's read-only
  render.
- **FE-C — Sticky sidebar.** Hızlı Bilgiler (dates, vessel specs incl. Beam/Draft JD-4, marina), **Map mini-card**
  (reuse `LocationMap`), embedded **İş Mesajları** thread (reuse the Messages thread+composer, compact) + "Mesajlar"/
  "Belgeler" shortcuts.
- **FE-D — Work Logs** (JD-3): chronological cards (date/duration/technician note) + "Çalışma Kaydı Ekle" (only if the
  add endpoint exists).
- **FE-E — Documents/Evidence gallery** (JD-5): SR attachments + work-log/completion evidence, signed read-url →
  lightbox (reuse); "Fotoğraf/Belge Yükle" via the signed-URL flow.
- **FE-F — Activity timeline** (JD-1): durable events (assigned/started/submitted/completed) with timestamps + actor
  role. No ephemeral SignalR-only entries.
- **FE-G — Completion flow** (JD-2): "Tamamlamayı Gönder" with a confirmation/review step; read-only banners for
  CompletionSubmitted ("Onay Bekleniyor") and Completed ("İş Tamamlandı" summary). No invoice/payout.
- **FE-H — States + realtime.** Loading skeletons, error/not-found/unauthorized/profile-not-linked, closed banner;
  reuse SignalR (`MessageAdded`, status changes) to refetch. Derived "Gecikmiş/Yaklaşıyor" schedule chip (client
  computed, shown separately from the real status).

## Suggested execution order (highest value first)
1. **JD-1** (aggregate) + **FE-A/FE-B** — the workspace becomes real (header, stepper, scope, offer, sidebar quick-info).
2. **JD-2** + **FE-G** — start/complete actions + completion states (the operational core).
3. **JD-3** + **FE-D** (work logs) and **FE-C** map + embedded messages.
4. **JD-4** (Beam/Draft) + **JD-5** + **FE-E** (evidence) + **FE-F** (activity timeline).

## Reality flags to keep (do not let the UI over-promise)
- No **Pause/Resume/Schedule/Material** commands → those actions are not shown; states render read-only.
- No **provider-notes edit** command → notes are read-only.
- No **rating / revenue / invoice / payout** (no Review/Payment contract) → omit; not faked.
- **Customer PII never shown** → "Tekne Sahibi" role label only; attachment access authorization-scoped.
- Vessel Beam/Draft need **JD-4**; Length/Year/Material already enriched. Amount = the provider's own accepted-offer
  total only (never a customer budget).
