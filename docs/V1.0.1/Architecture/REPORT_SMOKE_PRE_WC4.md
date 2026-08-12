# REPORT — pre-WC4 Messaging-only end-to-end smoke

> Runs `SMOKE_PRE_WC4_MESSAGING_ONLY.md` against the running stack, flags **ON**, owner `qa.owner.aug5@inktavia.com` +
> provider `provider2@inktavia.com` (portal OTP). The overriding criterion — **F: `sr.Messages` receives ZERO new chat /
> System rows** — is **GREEN**. **Result: PASS → WC4 is safe.** Nothing committed; no credentials changed.

## Preconditions (confirmed)
- Flags ON: `Messaging__WriteCutover__ChatMessages=true` on all 3 BFFs; `Messaging__WriteCutover__SystemMessages=true` on
  `service-request-api`.
- Stack healthy (messaging-api, service-request-api, payment-api, 3 BFFs, provider portal :3002, Redis, RabbitMQ, postgres).
- **Deploy note:** the running `service-request-api` (image 2026-08-11 19:27) **predated WC3b/c/d** (build-only until now),
  so Check D initially fell back to `sr.Messages`. Per the doc's "rebuild if a check falls back to SR", messaging-api +
  service-request-api were rebuilt `--no-cache` and recreated (picking up the WC3b `RemoteCalls__IServiceRequestMessagingRemoteCall__BaseUrl` env). After deploy, D passed. This deploys the uncommitted WC3b/WC3c/WC3d changes to the running stack (still uncommitted).
- **Baseline `sr.Messages`:** chat (SenderType 1,2) = **56**, System (SenderType 4) = **7**, total = **68**.

## Checks — all green (SR 55 / conv 27, owner 100029 · provider 100011)

| # | Check | Result |
|---|-------|--------|
| **A** | **WC3d** — provider Request-detail reads from Messaging | Provider portal `/app/service-requests/55` "Mesajlar & Aktivite" panel renders the full thread **including messages that exist only in Messaging** (native provider reply + realtime probe, never written to `sr.Messages`) — proving it reads via the repointed `messagingThread`. Identical to the Messages section. ✅ |
| **B** | **WC2/WC3a** — owner↔provider chat, Messaging-only | Owner **text/location/image** (API) + provider **text** (portal): each landed as **one native Messaging row (SourceKey NULL)**, **no new `sr.Messages` row**. Visible on the counterparty (owner's text/location/image render on the provider screen). **Provider realtime:** an owner API-send appeared on the provider's open chat within ~1s, no refresh. **Image** resolves via the **two-store read-url from Messaging** (owner read-url 200, real MinIO URL); a **request attachment** (SR 9011) still resolves **from SR** (fallback, 200); bogus fileId → 400. ✅ |
| **C** | **WC1** — System lifecycle, Messaging-only | Owner cancel on SR 54 produced a **Messaging** System message (conv 26, Type=3 StatusChange, SourceKey `sys:54:CONVERSATION_CLOSED`, SenderRole=System) and **no new `sr.Messages` System row** (sr_sys stayed 7). Flag ON ⇒ Messaging is the sole System producer. ✅ |
| **D** | **WC3b** — dispute transcript from Messaging | Opened a dispute on SR 55 (which has post-cutover native chat). Owner dispute case transcript went from **6** (pre-deploy, `sr.Messages`) to **21** (post-deploy, Messaging) and now **includes the post-cutover messages** ("PRE-WC4 provider reply native", "REALTIME PROBE", "owner text A"). The Messaging internal transcript endpoint is `[AllowAnonymous]` (200). `GetDisputeCaseDetail` is the single shared handler (DTO unchanged) → admin + provider surfaces consume the identical Messaging-sourced transcript. **(Owner surface directly verified; admin/provider by the shared-handler design — not separately clicked through.)** ✅ |
| **E** | **MO9** — notifications | Each real chat message fired a `NewMessageReceived` (Type 200) to the **counterparty**: owner→provider (#180 "New message from QA Owner Aug5"→100011), provider→owner (#179 "New message from PROVIDER 2 AS"→100029). The System cancel fired **no** chat notification (silent). Dispute-open fired Type 140 to owner/provider/admin. ✅ |
| **F** | **THE GATE — `sr.Messages` untouched** | **Final counts: chat=56, System=7, total=68 — IDENTICAL to baseline.** Zero new chat or System rows across the entire run (owner text/image/location ×4, provider text, realtime probe, System cancel, dispute open, post-deploy owner text). ✅ **DECISIVE — WC4 is safe.** |

## Pass criteria
1. **A–E green** on the surfaces exercised (owner + provider directly; admin via the shared dispute handler + audit-source). ✅
2. **F: `sr.Messages` chat + System counts unchanged** (56 / 7 → 56 / 7). ✅ — the decisive criterion.
3. Provider realtime, notifications, dispute transcript, and two-store read-url all sourced from Messaging; SR-attachment
   fallback intact. ✅

**No surface wrote `sr.Messages` for chat/System with the flag ON, and no chat read fell back to SR (after deploying
WC3b/c/d).** → **PASS.**

## Actions taken on the QA stack (normal app actions)
- Owner sends to SR 55: 4 texts, 1 location, 1 image (all native Messaging). Provider reply on SR 55. Owner cancel of
  SR 54 (now Cancelled/status 90). Dispute opened on SR 55 (id 1). These are normal app actions; not reverted.
- Rebuilt + recreated `messaging-api` + `service-request-api` with the uncommitted WC3a→WC3d changes (to deploy WC3b for
  Check D). Compose `docker-compose.yaml` carries the WC3b remote-call env (uncommitted). **Nothing committed.**

## Next — WC4 (Phase-4 close)
Gate green. Proceed to **WC4**: retire the SR→Messaging **sync consumer**, freeze the `sr.Messages` **write path** (make
`Messaging:WriteCutover:ChatMessages` permanently ON / remove the OFF branches), and drop the per-SR write **semaphore**
(the WC0 partial-unique index on `(ConversationId, SourceKey)` is the sole idempotency guard) — unifying chat on the
canonical Messaging store.
