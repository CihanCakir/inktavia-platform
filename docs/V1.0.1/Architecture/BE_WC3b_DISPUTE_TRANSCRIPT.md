# BE_WC3b — dispute case transcript → Messaging store

> **Repo:** `addesso-project` (Messaging + ServiceRequest). Phase-4 **WC3b** per `WC3_PLAN_READER_MIGRATION.md`: move
> the dispute case **chat transcript** off `sr.Messages` onto the Messaging store, so that after WC4 freezes
> `sr.Messages` the dispute case still shows the **complete** (post-cutover) conversation. Requires WC0–WC3a.
> Additive; identity/access unchanged. **Do not commit.**

## Fork — RESOLVED = (a) SR composer fetches from Messaging (one backend change, no FE)
The dispute case transcript (`DisputeCaseMessageDto`, produced by `DisputeCaseComposer` from `sr.Messages`) is consumed
by **three** surfaces: **admin** (AdminPanel BFF), **owner** (MO5 `MobileDisputeDtos`), **provider**. So fetching the
transcript **inside the SR composer** keeps the dispute-case DTO shape and fixes **all three** with **one backend
change and zero FE changes**. Fork (b) (each FE loads the transcript from Messaging separately) would mean three FE
changes — rejected. The mild SR→Messaging read coupling is acceptable: it's a **read-only** cross-context fetch for a
composed admin/owner/provider view (like reading reference data); the decoupling goal is about **write/ownership**
(Messaging owns chat — unchanged).

## Baseline (investigated)
- `DisputeCaseComposer` maps `sr.Messages` → `DisputeCaseMessageDto` (`Id, SenderUserId, SenderType, MessageType,
  Content, AttachmentFileId, …`) inline into the dispute case.
- The Messaging store holds the complete transcript (sync mirror OFF / native writes ON), with attachments as
  `MessageAttachmentEntity.FileStorageId` (= the fileId string) and location in Content-JSON + the WC0 columns.
- SR has **no** Messaging remote-call yet (it has reference-data/file-storage ones) — this is net-new.

## BE — WC3b
1. **Messaging internal transcript endpoint (S2S):** `GET` conversation transcript **by context**
   `(ContextType=ServiceRequest, ContextId=srId)` → the ordered messages with `{ SenderUserId, SenderRole,
   MessageType, Content, AttachmentFileStorageId (first attachment), LocationLat/Lng/Label, SentAt }`. **Service-to-
   service** (authenticated by the caller's service token — the established pattern; the endpoint is internal, the
   dispute case enforces its own access at its own layer). Not participant-scoped (the SR module is the caller, not an
   end user).
2. **SR → Messaging remote-call (net-new):** an `IServiceRequestMessagingRemoteCall` (`IAizenRemoteCall`) on the SR
   module → the transcript endpoint. Auth via the SR host's outbound service token (consistent with the S2S pattern;
   base URL `messaging-api` in compose + `depends_on`).
3. **`GetDisputeCaseDetail` handler:** fetch the Messaging transcript for the SR **before** composing, and pass it into
   `DisputeCaseComposer` so it maps **that** (not `sr.Messages`) → `DisputeCaseMessageDto`. Map
   `AttachmentFileStorageId` (string) → `AttachmentFileId` (Guid) via `Guid.TryParse`; map the Messaging role → the SR
   `SenderType`. Keep ordering by timestamp. The rest of the dispute case (economics, lifecycle, evidence) is unchanged.
4. **No flag needed:** the transcript read is safe regardless of the WC2/WC3a write flag — Messaging is the complete,
   canonical read store already (sync keeps it complete with the flag OFF; native writes with it ON), exactly as the
   provider/owner chat reads were cut over without a flag. The sync remains the safety net until WC4.

## Don't-break / QA
- Additive: a Messaging internal transcript endpoint + a net-new SR→Messaging remote-call + the composer sourcing the
  transcript from it. The dispute case DTO shape + all three consuming surfaces are **unchanged** (they just get the
  correct, complete transcript). Economics/lifecycle/evidence composition untouched.
- The transcript now includes **post-cutover** chat (which `sr.Messages` no longer has) + **pre-cutover** chat (synced
  into Messaging) → complete on all three surfaces.
- Tests: (1) SR + Messaging + solution build 0 errors; (2) the Messaging transcript endpoint returns the ordered
  conversation for an SR context, S2S-authed; (3) the dispute case transcript now sources from Messaging (a message
  sent post-WC2/WC3a appears in the dispute case; attachment fileId maps string→Guid; location + system messages
  present); (4) admin + owner + provider dispute case still render the transcript (DTO unchanged); (5) a dispute on an
  SR with only old (synced) chat still shows it. Live smoke (open a dispute, send a chat message post-cutover, confirm
  it appears in the admin + owner + provider dispute case) needs the stack — document it.

## Report
`docs/V1.0.1/Architecture/REPORT_BE_WC3b_DISPUTE_TRANSCRIPT.md`: the Messaging internal transcript endpoint, the
net-new SR→Messaging remote-call (service-token auth), the `GetDisputeCaseDetail`/composer switch (Messaging transcript
→ `DisputeCaseMessageDto`, fileId string→Guid), and the tests confirming all three dispute surfaces show the complete
post-cutover transcript. Then **WC3c** (retire the legacy SR chat read-endpoints — confirm dead + remove) → **WC4**
(retire the sync + freeze `sr.Messages`).
