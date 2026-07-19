# JB-3 + JB-4 — Backend: Weekly workload + Action-required feed

Feeds the Jobs dashboard's **analytics** (weekly workload bar) and the **"Aksiyon Bekleyen İşler"** panel. Both are
provider-scoped, grounded in the same assignment+SR data JB-1/JB-2 use. Run JB-3, then JB-4 (independent — either order
is fine). Reuse the JB-1/JB-2 patterns exactly (in-module SR join; BFF bulk vessel enrichment; provider id from the
assertion; one query, no N+1; no cross-module module-to-module calls).

---

## Phase JB-3 — Weekly workload aggregation (stacked bar chart)

New `GET /provider/jobs/workload?weeks=6` → per-week buckets for the next N weeks (default 6), each with counts of
`{ scheduled, inProgress, completed }`.

### Module — `GetProviderJobsWorkloadQuery(int weeks)`
- Over the caller provider's assignments (join SR for status/dates, in-module, like JB-2).
- Bucket by ISO week starting **this week** (UTC, Monday-start — pick one convention and document it). For each of the
  next `weeks` buckets:
  - `scheduled` = jobs whose `scheduledStartDate` falls in that week and status ∈ {Assigned, Scheduled}.
  - `inProgress` = jobs whose `scheduledStartDate` (or actualStartDate) falls in that week and status = InProgress.
  - `completed` = jobs whose `actualEndDate` falls in that week and status = Completed.
- Response: `{ weeks: [ { weekStartUtc, label ("H42" or "12 Eki"), scheduled, inProgress, completed } ] }`.
  (The SPA renders; the server decides the buckets. Keep the label as a short code the SPA can also reformat.)
- Do the bucketing in memory over the (already provider-filtered) rows, or in SQL — either way one query, no N+1.
  Dates Postgres-safe UTC.

### BFF — passthrough
`GET /provider/jobs/workload?weeks=6` → `GetProviderJobsWorkloadBffQuery` → module. Provider-scoped. No enrichment
(counts only). Refit method with the `weeks` query param.

### Acceptance — JB-3
- Returns exactly `weeks` buckets (default 6) with the three series; empty weeks are zero-filled (not omitted).
- Provider-scoped; one query; counts match the jobs the provider actually has in those weeks.

---

## Phase JB-4 — Action-required feed (urgent panel)

New `GET /provider/jobs/action-required` → the jobs needing intervention, **grouped** and **enriched** (title +
vessel, via the JB-1 mechanism).

### Groups (map to the design's three sub-cards)
| Group | Statuses |
|-------|----------|
| `ownerApproval` | `WaitingForOwnerApproval`, `CompletionSubmitted` |
| `materialRequired` | `WaitingForMaterial` |
| `blocked` | `Paused` |

### Module — `GetProviderJobsActionRequiredQuery`
- Over the provider's assignments joined to SR (in-module): select those whose status ∈ the union above.
- Project each item: `assignmentId, serviceRequestId, status, title, requestCode, vesselId, scheduledStartDate`.
- Return them grouped (or flat with a `group` field the BFF/SPA buckets). Cap each group (e.g. top 5 by
  scheduledStartDate) — the panel shows a few, not everything.

### BFF — enrich + expose
`GET /provider/jobs/action-required` → module call, then **bulk vessel-enrich** the items (one `GetSummaries` call,
same as JB-1) to fill `vesselName`. Response grouped: `{ ownerApproval: [...], materialRequired: [...], blocked: [...] }`.

### CTAs — decide, don't assume
The design's buttons are "Onay Hatırlat" (remind owner), "Stok Kontrol" (check stock), "Engeli Çöz" (resolve block).
**Confirm which are real backend commands vs navigation:**
- If a "remind owner" / "resume job" command already exists, wire the CTA to it.
- If not, the CTA is **navigate-to-detail** (`/app/jobs/:assignmentId`) for MVP — do NOT invent a command. Mark in the
  report which CTAs are real vs navigate-only.

### Acceptance — JB-4
- Returns the three groups, each enriched with title + vessel, capped, provider-scoped. Empty groups → empty arrays.
- One bulk vessel call (no N+1). No customer identity. Statuses stay codes (SPA localizes).

---

## Constraints (both)
- Provider-scoped; ownership in the module (assertion → ProviderProfileId). No client-sent profile id.
- Modules never call each other — SR fields via in-module join; vessel name is the only BFF enrichment.
- One query / one bulk vessel call; dates UTC/Postgres-safe; no customer identity; status codes stay codes.

## Report
Append to `REPORT_BACKEND.md` ("JB-3", "JB-4"): the 6 workload buckets for the seeded provider, and the three
action-required groups (enriched), plus which CTAs are real commands vs navigate-only. Unfinished is **not done**.

## Frontend (I will build after these land)
FE-2 analytics: **install recharts**, render the weekly workload **stacked bar** (JB-3) + the status **donut** (from
JB-2, already available). FE-3: the action-required panel's three sub-cards (JB-4) with the confirmed CTAs.
