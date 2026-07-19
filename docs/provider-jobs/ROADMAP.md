# İşler / Jobs Dashboard (SCREEN_55) — Backend + Frontend Roadmap

The approved design is a full operational dashboard (KPIs, two charts, an action-required panel, an enriched tabbed
table). It is **well beyond** what the current Jobs backend provides. This roadmap phases the work, grounded in what
exists vs what must be built.

## What exists today
- `GET /api/v1/provider/…/jobs` (paginated) → `ProviderJobDto { assignmentId, serviceRequestId, serviceRequestOfferId,
  status, scheduledStartDate/EndDate, actualStartDate/EndDate, providerNotes }` + `{ hasProfileLink, pageIndex,
  pageSize, totalReturned, items }`.
- **No** job title, vessel name, human code, or amount. **No** global counts / aggregation / charts data.
- Frontend: **no chart library installed** (recharts/chart.js absent).

## Design → data → status (gap analysis)
| Design section | Data needed | Status |
|---|---|---|
| KPI board (Aktif/Devam/Planlanan/Onay Bekleyen) | **global** counts per status | build — **JB-2** |
| "İş Durumu Dağılımı" donut | counts per lifecycle state (global) | build — **JB-2** |
| "Haftalık İş Yükü" stacked bar (6 weeks) | per-week × {Scheduled, InProgress, Completed} | build — **JB-3** |
| "Aksiyon Bekleyen İşler" panel (Owner Approval / Material / Blocked) | jobs in those states + vessel + title | build — **JB-4** |
| Table "İş (title/code)" + "Tekne (vessel)" | title (from SR), vessel name (from Vessel), human code | build — **JB-1** |
| Table Yaşam Döngüsü / Durum / Planlanan / Aksiyon | status + dates | **exists** (GET /jobs) |
| Tabs (Atananlar/Planlananlar/…) + "Daha fazla yükle" | status filter + pagination | mostly exists — **JB-5** |
| Amount / revenue | offer amount enrichment | out of scope (Post-MVP) |

Note: there is **no "Blocked" status** — the SR lifecycle has `Paused` and `WaitingForMaterial`. "Blocked" in the
design maps to **Paused**. Customer identity is never shown; vessel/title only.

---

## Backend roadmap (phased, provider-scoped)

### JB-1 — Jobs list enrichment (title + vessel + code)
Mirror the detail **vessel-enrichment** pattern (one bulk call, id→response mapping; module services never talk to
each other — the BFF orchestrates). Enrich each job with: `title` (from the ServiceRequest), `vesselName` (from the
Vessel bulk summary), and a human `code` (reuse the SR `requestCode`, e.g. `SR-…`; the design's "OP-4492" is
cosmetic — either add an assignment code or reuse requestCode). Add these to `ProviderJobDto`. Powers the table's
İş/Tekne columns and the action panel's vessel names.

### JB-2 — Jobs summary counts (global)
New `GET /jobs/summary` → counts per lifecycle state **across all of the provider's jobs** (not the paginated page):
`Assigned, Scheduled, InProgress, WaitingForOwnerApproval, WaitingForMaterial, Paused, CompletionSubmitted,
Completed` + derived `active` (all non-Completed) + `total`. One grouped query (no N+1). Powers the KPI board and the
donut. (The paginated endpoint can't produce globals — this is the missing piece the design's KPI skeleton note
refers to.)

### JB-3 — Weekly workload aggregation
New `GET /jobs/workload?weeks=6` → 6 week buckets, each with counts of `{ scheduled, inProgress, completed }`.
Bucket by `scheduledStartDate` (scheduled/in-progress) and `actualEndDate` (completed). Powers the stacked bar chart.
Server does the bucketing (dates are Postgres-safe UTC); the SPA only renders.

### JB-4 — Action-required feed
New `GET /jobs/action-required` → the jobs needing intervention, grouped: **Owner Approval** (`WaitingForOwnerApproval`
+ `CompletionSubmitted`), **Material Required** (`WaitingForMaterial`), **Blocked** (`Paused`). Each item enriched
(title + vessel) via JB-1's mechanism. Powers the urgent panel. CTAs: "Onay Hatırlat" / "Stok Kontrol" / "Engeli Çöz"
— **confirm which are real commands** (a reminder-to-owner command? a resume command?) vs. navigation-only for MVP;
mark the ones without a backing command as navigate-to-detail for now.

### JB-5 — Status-filtered list + pagination
Ensure `GET /jobs` accepts a `status` filter (for the tabs: Assigned/Scheduled/InProgress/waiting-group/Completed) and
keeps `pageIndex/pageSize`. Keep "Daha fazla yükle" (no grand total is exposed; JB-2 gives the counts the UI needs).

**Constraints (all):** provider-scoped, ownership in the module; no customer identity; one bulk enrichment call (no
N+1); dates UTC/Postgres-safe; codes stay codes (SPA translates status labels); no new totals math on the client.

---

## Frontend roadmap (phased)

### FE-1 — Shell + header + KPI board
Page shell, header ("İşler" + subtitle), **Liste/Pano** toggle, **Yenile**. 4 KPI cards from **JB-2** (skeleton until
JB-2 lands; the paginated endpoint alone cannot fill them).

### FE-2 — Analytics (needs a chart lib)
**Install `recharts`** (none today). Weekly workload **stacked bar** (JB-3) + status **donut** (JB-2). Nautical
palette (navy/gold/neutral). Until JB-3/JB-2, render empty/skeleton — do not fake numbers.

### FE-3 — Action-required panel
3 sub-cards (Owner Approval / Material / Blocked) from **JB-4**, each with vessel + title + a CTA (real command or
navigate-to-detail per JB-4's decision).

### FE-4 — Jobs table (enriched, tabbed)
Tabs: Tüm İşler · Atananlar · Planlananlar · Devam Edenler · Bekleyen/Bloke · Tamamlananlar (JB-5 filter). Columns:
**İş** (title + code, JB-1) · **Tekne** (vessel, JB-1) · **Yaşam Döngüsü** (mini-stepper Assigned→InProgress→Completed)
· **Durum** (localized badge) · **Planlanan** (date range) · **Aksiyon** (Detay → `/app/jobs/:assignmentId`). "Daha
fazla yükle". This is buildable partly now (status/dates/stepper) and fully after JB-1.

### FE-5 — Pano (Kanban) view
Status columns (Planlandı · Devam Ediyor · Bekleyen/Bloke · Tamamlandı) from the same data; Liste/Pano toggle.

---

## Suggested execution order (highest value first)
1. **JB-1** (enrichment) + **JB-2** (counts) — unlock the table + KPIs, the core of the page.
2. **FE-1** (KPIs) + **FE-4** (enriched table) — the dashboard becomes real and useful.
3. **JB-4** + **FE-3** (action panel) — operational value.
4. **JB-3** + **FE-2** (charts, +recharts) — analytics polish.
5. **FE-5** (Pano) — optional process view.

## Data-reality flags to keep (do not let the UI over-promise)
- No amount/revenue anywhere (no offer-amount enrichment) — omit the money column; revenue KPI is Post-MVP.
- No "Blocked" status → Paused. No customer identity → vessel/title/code only.
- No grand-total count → "Daha fazla yükle", not "X / Y".
- Charts require a new lib (recharts) + new aggregation endpoints (JB-2/JB-3) — they are **not** free from GET /jobs.
