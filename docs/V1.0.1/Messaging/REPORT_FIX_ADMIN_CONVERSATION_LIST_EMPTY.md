# REPORT — admin conversation list returns empty: diagnosis + fix

**Spec:** `docs/V1.0.1/Messaging/FIX_ADMIN_CONVERSATION_LIST_EMPTY.md` (diagnostic-first).
**Repo:** `addesso-project` (Messaging module + AdminPanel BFF) and `inktavia-marine-admin-web` (FE mapping).

## TL;DR
The empty list is **NOT** a DB split, token-keyed routing, or query scoping. The runtime data is present and the
module returns it for the BFF service token. The real cause is a **BFF response-envelope deserialization mismatch**:
the Messaging module controllers return the wrapped `AizenApiResponse` envelope (`{ header, body:{ items, total } }`),
but the BFF's Refit remote call declared the **bare** `GetConversationListResponse`, so Refit found no top-level
`items`/`total` and deserialized to `Total=0, Items=null`. Fixed by making the messaging remote calls return
`AizenApiResponse<T>` (mirroring the working Vessel admin path) + FE mapping for the field-name/nesting differences.
The module query was left **unscoped/unchanged**; auth was not weakened.

## Diagnosis (runtime)
1. **Which DB does the Messaging module use vs a working module (Payment)?** Both resolve to **`inktavia_store`**.
   - messaging-api effective connection (`DatabaseSettings:Messaging:ConnectionString`, from
     `appsettings.Development.json`) = `Host=postgres;Database=inktavia_store`. (Note: the compose env
     `ConnectionStrings__Messaging` is under a key `AddAizenUnitOfWork` does not read — harmless here because
     Development.json already points `DatabaseSettings` at `inktavia_store`.)
   - payment-api uses `DatabaseSettings:Payment:ConnectionString` = `inktavia_store`. Same DB instance + name.
2. **Row counts (`WHERE "IsDeleted"=false`):**
   - `inktavia_store.messaging.conversations` = **4** (seeded SR #9001, #9004, #9010 + a W1 test convo). ← runtime reads here.
   - `aizen.messaging.conversations` = 3 (a legacy parallel DB; NOT read by runtime).
   - `postgres` DB has no `messaging` schema.
   So the seed landed in the DB the runtime module reads — no aizen↔inktavia_store split for messaging.
3. **Is it token-keyed routing?** No. `AddAizenUnitOfWork` registers a DbContext with a **fixed** connection string
   (not per-request/token-resolved). Decisive test: I minted the **admin-panel-bff client-credentials service token**
   (exactly what the BFF injects) and called the module directly — `GET /api/v1/conversations` returned **`total:4`**.
   The module returns rows for the service token too, so neither the module, the DB, nor the query is the cause.

## Actual cause (BFF layer)
The module `ConversationsController.GetList` returns `AizenApiResponse<GetConversationListResponse?>` via
`SetResponse(...)` → the HTTP body is `{ "header": {...}, "body": { "items": [...], "total": 4 } }`. But
`IAdminMessagingBffRemoteCall.GetConversationsAsync` declared `Task<GetConversationListResponse>` (bare). Refit's
`SystemTextJsonContentSerializer` deserialized the envelope into the bare type, found no top-level `items`/`total`
(they are nested under `body`), and produced `Total=0, Items=null` — the empty list. The `AizenHttpClientFactory`
does not unwrap; there is no `body`-unwrapper.

**Why Payment works and messaging didn't (the outlier):** Payment's admin controllers return the payload **bare**
(e.g. `ProviderPlanController.GetAll` → `return Ok(result)` → `[...]`; `/payment/transactions` →
`{items,total,page,pageSize}`), which matches the bare Refit return types. Messaging's controllers **wrap** — so its
remote calls had to deserialize the envelope. The **Vessel** admin path is the exact working precedent for a wrapped
module: `IVesselAdminBffRemoteCall` returns `Task<AizenApiResponse<T>>` and the handler reads `result.Body`.

## Fix applied (mirror Vessel; query untouched)
1. **`IAdminMessagingBffRemoteCall`** — the data-returning calls now return the envelope:
   `Task<AizenApiResponse<GetConversationListResponse>>` / `<GetConversationDetailResponse>` / `<SendMessageResponse>` /
   `<GetModerationQueueResponse>` (added `using Aizen.Core.Infrastructure.Api;`).
2. **`AdminMessagingController`** — the matching actions return the module envelope straight through
   (`return result;`) instead of double-wrapping via `SetResponse`, preserving the module header + body. The FE's
   existing `unwrap()` reads `body`.
3. **FE mapping** (`inktavia-marine-admin-web/.../useMessagesQuery.ts`) — the module DTOs use different field
   names/nesting than the FE types, so the list rows would otherwise render blank previews/badges/timestamps and the
   detail wouldn't load:
   - list item: `preview→lastMessagePreview`, `timestamp→lastMessageAt`, `unreadCount→unreadCountByAdmin`, stringified
     ids → numbers.
   - detail: unwrap the module's `body.conversation` nesting; map messages `timestamp→sentAt`, ids→numbers,
     `moderationReason`/`location` default null.
- The Messaging **module query was NOT changed** (still `!IsDeleted` + optional status/contextType, unscoped). Auth
  unchanged.

## Verification (on-screen)
Fresh admin login on `/app/messages`:
1. ✅ The list shows **4 conversations** — "Hull Cleaning & Inspection — SR #9001", "Engine Overhaul & Fuel System —
   SR #9004", "Navigation Electronics Calibration — #9010" (+ the W1 test convo) — with the ALL/PENDING/FLAGGED tabs,
   the "4" count, previews, an unread badge (13), and timestamps. Status dot **Live** (Wave-1 socket still connected).
2. ✅ Selecting a conversation loads its messages (the W1 convo shows its 13 messages with sender, timestamps, and
   moderation badges); the Wave-1 live frame refetch now shows real content.
3. ✅ Payment admin unaffected — the Packages page renders 3 provider plans (Free / Standard ₺499 / Premium ₺999),
   3 participant plans, 6 total. No regression.
- Backend builds clean (AdminPanel BFF); FE typecheck + lint clean.

## Notes
- Not fixed (out of this task's scope, not on the W1 verification path): the messaging BFF `attachment-upload-url` and
  `reports` remote calls still return `object` (they carry the same envelope; wire them to `AizenApiResponse<T>` when
  those panels land in a later wave).
- The W1 test conversation (`9900000001`, with a `UserId=0` participant) is leftover dev scaffolding from Wave 1; the
  proper seeded demo data is SR #9001/#9004/#9010.
