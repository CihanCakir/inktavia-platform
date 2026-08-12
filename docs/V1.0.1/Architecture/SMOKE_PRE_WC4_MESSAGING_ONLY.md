# SMOKE — pre-WC4 Messaging-only end-to-end gate

> **Run in Claude Code against the running stack.** This is the **gate before WC4** (which removes the SR write
> fallback — the point of no return). Goal: prove the **Messaging-only** chat path is fully green with the write flag
> **ON**, so that retiring the sync + freezing `sr.Messages` in WC4 is safe. **The single overriding pass criterion:
> `sr.Messages` receives ZERO new chat rows throughout.** Read-only except normal app actions + the flag. **Nothing to
> commit.**

## Preconditions
- Flags **ON**: `Messaging:WriteCutover:ChatMessages` (all three BFFs) + `Messaging:WriteCutover:SystemMessages` (SR
  host). Confirm + restart if needed (env changes aren't hot-reloaded).
- Stack up (messaging-api, service-request-api, payment-api, the 3 BFFs, provider portal :3002, admin :3000, owner
  mobile), Redis + RabbitMQ healthy. **Deploy by behaviour, not strings** (the recurring stale-image gotcha — a clean
  `--no-cache` rebuild if any check falls back to SR).
- Accounts: owner `qa.owner.aug5@inktavia.com` / `QaReset2026!!`, provider `provider2@inktavia.com` (portal OTP).
- **Baseline snapshot:** record `SELECT COUNT(*) FROM servicerequest.service_request_messages WHERE "SenderType" IN
  (1,2)` (Owner/Provider chat rows) **before** the run — it must be **unchanged** at the end.

## Checks

### A. WC3d — provider Request-detail reads from Messaging (the skipped visual)
Open a Request-detail page (provider portal) with an existing thread → the chat panel renders the thread (text / image
/ location / System pills, both parties, ordering, read state) **from Messaging** (`messagingThread`). ✅ if it renders
identically to the Messages section.

### B. WC2/WC3a — owner ↔ provider chat, Messaging-only
For **text**, **image**, **location**, each direction (owner→provider, provider→owner):
- Lands as **one native Messaging row** (SourceKey NULL) — **no new `sr.Messages` row**.
- Visible on the **counterparty** + the **admin** audit; **provider realtime** updates the open provider chat within
  ~1s (no refresh).
- Image resolves via the **two-store read-url from Messaging**; a **request/completion-evidence** attachment still
  resolves **from SR** (fallback intact).

### C. WC1 — System lifecycle, Messaging-only
Drive offer → accept → start → complete on one SR, cancel on another. Each System pill (`OFFER` / `OFFER_ACCEPTED` /
`JOB_STARTED` / `JOB_COMPLETED` / `CONVERSATION_CLOSED`) appears **once** on owner + provider + admin — and writes **no
new `sr.Messages` System row** (SystemMessages flag ON → Messaging is the sole producer).

### D. WC3b — dispute transcript from Messaging
Open a dispute on an SR, send a **post-cutover** chat message, then open the dispute case on **admin + owner + provider**
→ the transcript includes that post-cutover message (sourced from Messaging, not `sr.Messages`).

### E. MO9 — notifications
Each real owner/provider chat message fires a `NewMessageReceived` notification to the counterparty (owner bell +
provider); System/offer messages stay **silent**.

### F. THE GATE — `sr.Messages` untouched
Re-run the baseline count: `SELECT COUNT(*) FROM servicerequest.service_request_messages WHERE "SenderType" IN (1,2)`
→ **must equal the pre-run baseline** (zero new chat rows). Also confirm **no new System rows** were written during C.
This is the proof that nothing writes `sr.Messages` for chat with the flag ON → **WC4 is safe.**

## Pass criteria (record in the report)
1. A–E all green on owner + provider + admin.
2. **F: `sr.Messages` chat + System row counts unchanged** across the whole run (the decisive criterion).
3. Provider realtime + notifications + dispute transcript + two-store read-url all sourced from Messaging.
Any new `sr.Messages` chat/System write, or a surface falling back to SR, **blocks WC4** — investigate first.

## Report
`docs/V1.0.1/Architecture/REPORT_SMOKE_PRE_WC4.md`: per-check results (A–F), the before/after `sr.Messages` counts
(the gate), and any fallback/anomaly. On green → **WC4** (retire the sync consumer, freeze the `sr.Messages` write
path, make the WriteCutover flag permanently ON / remove the OFF branches, drop the per-SR semaphore — WC0 unique index
is the sole guard) → **Phase-4 closed**, chat unified on the canonical Messaging store.
