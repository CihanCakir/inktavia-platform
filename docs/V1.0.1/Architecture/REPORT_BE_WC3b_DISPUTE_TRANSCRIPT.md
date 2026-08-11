# REPORT — BE_WC3b dispute case transcript → Messaging store

> Implements `BE_WC3b_DISPUTE_TRANSCRIPT.md`. Moves the dispute case **chat transcript** off `sr.Messages` onto the
> canonical **Messaging** store, so that after WC4 freezes `sr.Messages` the dispute case still shows the complete
> (pre- + post-cutover) conversation. Fixes **all three** dispute surfaces (admin / owner / provider) with **one backend
> change and zero FE changes** — the dispute case DTO shape is unchanged. **Builds 0 errors. Not committed.**

## 1. Messaging internal transcript-by-context endpoint (S2S) — WC3b.1
New read-only, non-participant-scoped endpoint on the Messaging module:
- **Query + handler** `GetConversationTranscriptByContextQuery(ContextType, ContextId)` +
  `GetConversationTranscriptByContextQueryHandler` (Application/Query/GetConversationTranscriptByContext). Injects only
  `IConversationRepository`; resolves via the existing `GetByContextWithMessagesAsync` (Participants + Messages +
  Attachments). Returns the messages `!IsDeleted && !IsInternalNote && ModerationStatus != Blocked`, **ordered by
  `SentAt`**, projected to `TranscriptMessageDto`. A missing conversation returns an **empty** transcript (never
  throws/404) so a chat-less SR does not break the dispute case.
- **Response DTO** `ConversationTranscriptResponse { IReadOnlyList<TranscriptMessageDto> Messages }` (Abstraction).
  Each `TranscriptMessageDto` carries `MessageId, SenderUserId, SenderRole (int), MessageType (int), Content,
  AttachmentFileStorageId (first attachment), LocationLat/Lng/Label, SentAt`. **`SenderRole`/`MessageType` are the
  integer enum values on purpose** — a stable numeric cross-module contract, so the consuming module keeps its own
  local projection and there is no string/number enum-serialization ambiguity across the boundary.
- **Controller** `MessagingInternalController` — `[ApiController] [AllowAnonymous] [Route("api/v1/messaging/internal")]`,
  `GET conversations/by-context/transcript?contextType=&contextId=`, dispatched via `IAizenCQRSProcessor` (the Messaging
  convention). The `…/internal` prefix is meant to be excluded at the public API gateway.

## 2. SR → Messaging remote-call (net-new) — WC3b.2
- `IServiceRequestMessagingRemoteCall : IAizenRemoteCall` (SR **Abstraction**/RemoteCall) →
  `GET /api/v1/messaging/internal/conversations/by-context/transcript?contextType={contextType}&contextId={contextId}`,
  returning `AizenApiResponse<SrConversationTranscriptDto>`. Auto-discovered/registered by `AddAizenRemoteCall` (no DI
  wiring needed). Follows the existing SR remote-call convention (cf. `IServiceRequestReferenceDataRemoteCall`):
  **SR-local projection DTOs declared inline** (`SrConversationTranscriptDto`, `SrTranscriptMessageDto`) — the SR module
  does **not** reference `Messaging.Abstraction`, and passes `contextType` as the enum **member name** `"ServiceRequest"`
  (the endpoint's enum model-binding accepts the name).
- **Config:** `RemoteCalls__IServiceRequestMessagingRemoteCall__BaseUrl: http://messaging-api:8080` added to
  `service-request-api` in `docker-compose.yaml`.

## 3. `GetDisputeCaseDetail` sources the transcript from Messaging — WC3b.3
- The handler fetches the Messaging transcript for the SR (`ReadMessagingTranscriptAsync(sr.Id)`, contextType
  `"ServiceRequest"`) and passes it into `DisputeCaseComposer.Compose(...)`.
- `DisputeCaseComposer.Compose` gained an optional `IReadOnlyList<SrTranscriptMessageDto>? transcript` parameter. When
  non-null it is the source of the case's `Messages`; a new `MapMessage(SrTranscriptMessageDto)` maps each →
  `DisputeCaseMessageDto`:
  - **Role → SenderType:** `MapSenderType` (Owner/Provider/Admin/System by value; anything else → System).
  - **Type → MessageType:** `MapMessageType` (Text/SystemNotification/StatusChange pass through, **MediaAttachment(5) →
    Image**, Location(6) → Location; InternalNote is filtered at the endpoint so it never arrives).
  - **Attachment fileId:** `AttachmentFileStorageId` (string) → `AttachmentFileId` (Guid) via `Guid.TryParse`.
  - **Timestamp:** `SentAt.UtcDateTime` → `CreatedAt`.
- **Reversible safety-net:** if the Messaging read throws, the handler passes `null` and the composer **falls back to
  `sr.Messages`** (best-effort, mirroring the existing best-effort Payment read) — the case never loses its transcript.
  The sync keeps `sr.Messages` complete until WC4, so the fallback is safe today.
- **DTO shape unchanged:** `DisputeCaseMessageDto` / `GetDisputeCaseDetailResponse` are untouched, so admin (AdminPanel
  BFF), owner (MO5), and provider all get the corrected, complete transcript with **zero FE changes**. Economics /
  lifecycle / evidence composition is untouched. `sr.Messages` is still loaded by `GetByIdWithDetailsAsync` (other
  callers of that shared method rely on it) and simply serves as the fallback here.

## Deviation from the doc — auth
The doc specified the endpoint be "authenticated by the caller's service token — the established pattern". In reality
**module (Operation) hosts attach no outbound service token** (verified: the SR host wires `AddAizenRemoteCall` but no
outbound `DelegatingHandler` / client-credentials provider — the existing SR→ReferenceData calls work only because those
read endpoints are `[AllowAnonymous]`). So a token-gated endpoint would 401 the SR caller. The transcript endpoint
therefore follows the **actual established cluster-internal pattern**: `[AllowAnonymous]` under `api/v1/messaging/internal`
(gateway-excluded), read-only, returning only chat already visible to the conversation's participants; the dispute case
enforces its own access at its own layer (unchanged from when it read `sr.Messages`). This matches the prior I2
`[AllowAnonymous]` cluster-internal precedent. **Hardening option for later:** gate it with a shared-secret header via the
remote-call `DefaultHeaders` config (SR sends, Messaging validates) — deferred to avoid new fail-closed startup surface.

## Build / tests
- **0 errors:** Messaging host, ServiceRequest host, and the **full solution** (`Aizen.sln`, Release) — exit 0, zero
  error lines. (Both new controller returns carry the same benign `CS8619` nullability warning every existing
  `SetResponse` endpoint has.)
- **Unit tests (4/4 pass)** in `DisputeCaseComposerTests`:
  - existing: composes every part of the case; no cost/margin leak (§20.9) — unchanged (the 5-arg `Compose` still
    compiles via the optional param).
  - **new** `Transcript_sources_from_Messaging_and_maps_role_type_and_fileId`: the case's messages come from the passed
    transcript (**not** `sr.Messages`); role→SenderType, MediaAttachment→Image, string→Guid fileId, Location all verified.
  - **new** `Null_transcript_falls_back_to_sr_messages`: a null transcript falls back to `sr.Messages`.

## Live smoke (needs the running stack — not executed here)
Flag/ON-independent (Messaging is the canonical read store regardless of the WC2/WC3a write flag):
1. Open a dispute on an SR; send a chat message (owner/provider) **after** the WC2/WC3a cutover → it appears in the
   **admin + owner + provider** dispute case transcript (post-cutover message that `sr.Messages` no longer receives when
   the write flag is ON).
2. An image chat message shows with its `AttachmentFileId` (string→Guid) in the dispute case.
3. A dispute on an SR with only **old (synced)** chat still shows it (Messaging holds the synced transcript).
4. Bring `messaging-api` down briefly → the dispute case still renders via the `sr.Messages` fallback (no 500).

## Deferred / next
Transcript now reads from the canonical Messaging store on all three dispute surfaces. Next: **WC3c** (confirm the legacy
SR chat read-endpoints are dead, then remove) → **WC4** (retire the one-directional sync consumer + freeze `sr.Messages`
once the DB unique index is the sole idempotency guard).
