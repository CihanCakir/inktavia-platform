# 12 — Validation & Release Checklist

Each line is an observation, not an assertion. Unobserved = not done.

## Calculation (the crux)
- [ ] Server totals match a hand-computed fixture: multi-type items + percent discount spanning lines + KDV %20.
- [ ] A client-supplied `lineTotal` / `taxAmount` / `discountAmount` / `grandTotal` is **ignored** — server
      recomputes. Prove by sending wrong numbers and diffing.
- [ ] Money is `decimal` everywhere; no `float`/`double` in the money path. Rounding is round-per-line, documented.
- [ ] Quantity is `decimal` — 2.5 h persists and prices correctly.
- [ ] Discount is explicit (type+value), applied pre-tax, cannot drive the grand total below zero.
- [ ] Mixed currency rejected; unknown unit rejected; empty offer cannot be submitted.

## Lifecycle & concurrency
- [ ] One active offer per (provider, request). Submitted offer is immutable; change = withdraw + resubmit.
- [ ] Submit is idempotent (same key ⇒ one offer). Double-click cannot double-submit.
- [ ] Optimistic concurrency: a stale save is rejected with a reload path, not a silent overwrite.
- [ ] `SubmittedAt` / `ViewedAt` / `RevisionRequestedAt` are honest (set by the right transition/event).

## Auth & privacy (reuse discovery gates)
- [ ] End-user token never forwarded to modules; no `X-Aizen-User-Token`; assertion carries identity.
- [ ] `providerProfileId` never accepted from the client; sending one changes nothing (test it).
- [ ] Detail = access-check (biddable | has offer | assigned) else "not found"; other providers' offers never
      returned; only aggregate offer count.
- [ ] No owner PII beyond what the owner published; snapped coords only; exact berth hidden pre-authorization.
- [ ] Attachment signed URLs are short-lived, per-click; object keys/buckets never reach the SPA or the domain DB.
- [ ] `?access_token=` accepted only on `/hubs`; no secrets/tokens/exact-coords in logs; NetworkPolicy +
      non-empty `AllowedClientIds` in prod.

## Performance
- [ ] One detail aggregate; one vessel bulk call per detail (assert count); per-click signed URLs (no N+1).
- [ ] Auto-save debounced + cancellable; no request storm while typing.
- [ ] Draft not globally cached; concurrency token guards updates.

## Frontend
- [ ] No raw enum in the DOM; locale-aware money/date/number/tax/unit; **no budget**.
- [ ] Provisional totals replaced by server totals on save/preview/submit.
- [ ] Read-only submitted state; withdraw; preview matches server compute.
- [ ] States: skeleton/empty/error/disconnected/stale; unsaved-changes guard.
- [ ] No sample data in the bundle (grep `13.020`, `Jotun`, `Beneteau`).
- [ ] `typecheck`, `lint`, `build`, unit/component/E2E pass; a11y (keyboard table nav) + visual vs `screen.png`.

## Realtime
- [ ] viewed / revision-requested / rejected / message reach the provider; dedupe by id; a duplicate frame does
      not duplicate a timeline entry.
- [ ] Two-replica backplane re-verified after adding consumers.
- [ ] Timeline durable entries come from module state, not socket frames.

## Release
- [ ] Feature-flagged; old detail kept until parity. Metrics: detail p95, save/submit latency, auto-save rate,
      idempotency hits, realtime drops. Rollback = flag off; migrations additive.
