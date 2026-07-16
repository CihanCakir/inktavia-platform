# 07 — Target Architecture

## Layering (unchanged from discovery)

```
Browser (SPA) ──HTTPS──▶ Provider BFF ──service token + assertion──▶ ServiceRequest
     │                        │  ├──────────────────────────────────▶ Vessel (bulk summary, 1 call)
     │                        │  └──────────────────────────────────▶ FileStorage (signed read URLs, per click)
     └──WebSocket──▶ ProviderRealtimeHub ◀──── RabbitMQ ─────────────── ServiceRequest
                              │
                        Redis backplane (DB 15)
```

Auth is the canonical Provider flow (`docs/provider-service-request-discovery/06`), reused verbatim: end-user
token stops at the BFF; module identity via assertion; `ProviderProfileId` never from the client; `?access_token=`
only on `/hubs`. Mandatory production controls (NetworkPolicy, non-empty `AllowedClientIds`, no secrets in logs,
static-secret→signed-JWT migration recorded) all still apply — see `infrastructure/k8s/README.md`.

## Detail read — one aggregate response

The detail page loads a **single aggregate** (`GET .../{id}/detail-full` or an extended existing detail), not a
dozen calls. It contains: request header, work-scope items, approximate location, attachment metadata (ids +
counts, **not** signed URLs), timeline/messages (or a lazy sub-fetch), and **the caller's own offer with items
and server-computed totals**. Vessel specs are **BFF-enriched** (one Vessel bulk call) and merged in. Signed read
URLs for photos/documents are minted **on click**, one endpoint, short-lived — never embedded in the aggregate.

Rationale: the mock is dense but mostly one request's data; N calls per section would be slow and N+1-prone. Heavy
or rarely-opened sections (full message history, version history) may lazy-load.

## Offer editing — hybrid, aggregate-save biased

The module has **no fine-grained item commands** today, and building four (add/update/remove/reorder) is
avoidable churn for MVP. **Decision: aggregate save.** The draft is edited client-side (add/edit/delete/reorder
rows in memory) and persisted with **one `SaveOfferDraft` command** carrying the full item list + commercial
terms. The server recomputes all totals and returns the authoritative offer.

- **Auto-save** debounces this aggregate save (~1.5 s after the last edit) with request cancellation.
- **Concurrency**: the offer carries a token; a stale save is rejected with a clear "reload" error, not a silent
  overwrite.
- **Submit** is a separate command, **idempotent** (idempotency key), transitioning Draft→Submitted and freezing
  the content. Double-click / retry cannot create two submissions.

Fine-grained item commands are **post-MVP** (only worth it if auto-save payloads become large or true collab
editing is needed) — recorded, not built.

## Totals — computed once, in the module

Every total (per-category, discount, subtotal, tax, grand) is computed by the module's offer calculation service
(doc 06 algorithm) on every save/submit and stored/returned. The BFF passes them through. The SPA shows
provisional totals while typing and **replaces them with the server's** on each save response.

## Vessel & files

Vessel specs: reuse `IProviderVesselRemoteCall` bulk summary (09b.1) with a single id — **no per-field call, no
new endpoint**. Attachments: reuse the FileStorage signed-URL pattern (onboarding) — mint a short-lived read URL
per clicked photo/document; bucket/object keys never reach the SPA or the domain DB.

## Realtime

Reuse the bridge. Add producers module-side and consumers BFF-side for: **offer submitted** (→ owner's channel —
but the provider portal only needs the ones addressed to the provider), **customer viewed offer**, **revision
requested**, **message added**. These target `provider:{profileId}` (offer/viewed/revision/message are
provider-specific), not the city group. Idempotent, deduped by id in the UI. The activity timeline is built from
**durable** module events (status history), not from ephemeral socket frames.

## Caching

Reference lists (units, tax rates if any, categories) — BFF cache, long TTL. **Never cache the mutable draft
globally** — it is per-provider, per-request, and changes every keystroke; React Query holds it client-side with
the concurrency token as the guard.
