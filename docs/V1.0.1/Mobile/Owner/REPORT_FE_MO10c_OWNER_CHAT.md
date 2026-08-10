# REPORT — FE_MO10c — owner chat (text + image + location) + realtime

> **Repo:** `inktavia-marine-mobile` (RN/Expo). **Status:** implemented, not committed. Closes **MO10** — the owner ↔
> provider chat now sits at parity with the provider side (text + image + location), the last MO1 stub is gone, and
> the owner side of the Messaging capability is complete.

## What was built

The FE consumes the already-built MO10a/b BFF surface (inbox + thread + text/image/location send + image read-url) and
the MO9b notification bell for live updates. Cost-free throughout: the chat renders only message text + image +
location — never economics.

### 1. API + hooks (`features/service-requests/`)
- **`api/chatApi.ts`** — typed client over the mobile BFF, all through the shared `httpClient` (so mock-mode aware for
  free) and `normalizeEnvelope`:
  - `fetchConversations(skip, take)` → `GET /api/v1/mobile/conversations` (inbox rows: title, last-message preview,
    unread count, channel-open).
  - `fetchChatThread(id)` → `GET /api/v1/mobile/service-requests/{id}/messages` (returns `{ counterpartyName,
    channelOpen, messages[] }`; messages defensively sorted oldest→newest).
  - `sendChatMessage(id, input)` → `POST …/messages` with **exactly one of** `{ content | attachmentFileId |
    locationLat/Lng(+Label) }`.
  - `fetchAttachmentReadUrl(id, fileId)` → `GET …/attachments/{fileId}/read-url` (short-TTL signed GET; empty url ⇒
    placeholder).
  - Enums carried as string NAMES: `senderType` ∈ Owner/Provider/Admin/System, `messageType` ∈
    Text/Image/Location/SystemNotification/StatusChange.
- **`api/useChat.ts`** — React Query hooks: `useConversations`, `useChatThread`, `useSendChatMessage` (invalidates the
  thread + the conversations inbox on success), `useAttachmentReadUrl` (lazy, one query per fileId, `staleTime` 4 min so
  thumbnails don't re-mint every render).
- **`core/api/endpoints.ts`** — added `SERVICES.CONVERSATIONS`, `SERVICES.MESSAGES(id)`,
  `SERVICES.MESSAGE_ATTACHMENT_URL(id, fileId)` (replaced the stale "chat coming-soon" endpoint stub).
- **`core/api/queryKeys.ts`** — added `services.conversations()`, `services.chatThread(id)`,
  `services.attachmentUrl(id, fileId)`.

### 2. MO1 stub removed
`features/services/screens/ServiceRequestDetailScreen.tsx` — the dashed **"Messaging arrives in an upcoming update"**
placeholder (a toast-only `TouchableOpacity`) is gone, replaced by a real **"Open chat"** button that navigates to the
chat screen (`OwnerChat`, passing the SR id). This was the last MO1 owner stub.

### 3. Chat screen — `features/service-requests/screens/OwnerChatScreen.tsx`
Opened from the SR detail; registered as `OwnerChat` in `ServicesNavigator` + `types.ts`.
- **Message list** — a `FlatList` that auto-scrolls to the latest; own vs counterparty bubbles keyed off `isOwn`; the
  counterparty name resolved from the thread's `counterpartyName` (the inbox gap noted in the spec is filled here).
  - **Text** bubbles (own = gold gradient, counterparty = dark), read ticks on own messages (`done` → `done-all` when
    `isRead`).
  - **System** messages (`senderType === 'System'` / `SystemNotification` / `StatusChange`) render as centered
    lifecycle **pills**.
  - **Image** messages — thumbnail resolved lazily per message via `useAttachmentReadUrl` (cached), tap → full-screen
    viewer (`Modal`), placeholder on empty/failed url, optional caption.
  - **Location** messages — a map-style card (pin + label + coordinates) → tap → **native maps** (Apple Maps on iOS,
    Google Maps elsewhere) via `Linking`.
- **Composer** — text input + **attach image** + **share location**:
  - Image: `expo-image-picker` → **M4f client-presigned `directUpload`** (bytes go straight to storage, never through
    the BFF) → send `AttachmentFileId`.
  - Location: `expo-location` (permission-guarded, Expo-Go-safe) → best-effort reverse-geocode label → send
    `LocationLat/Lng/Label`.
  - **Optimistic append + rollback**: pending messages show immediately (local image uri / "Sending…"), drop on success
    (the invalidated thread refetches the real message), and flip to a **failed + Retry** row on error. Exactly-one-of is
    enforced client-side (each action sends a single field).
- **`core/utils/locationService.ts`** — `getCurrentSharedLocation()` (permission gate + reverse geocode, never throws),
  `mapsUrl()` / `openInMaps()` (platform-correct deep link).

### 4. Realtime (MO9b bell)
The screen subscribes to `onMobileNotification` on the shared `/hubs/notification` SignalR connection. A
`mobileNotification` frame (e.g. `NewMessageReceived`) is treated as a **hint** → refetch the open thread + invalidate
the conversations inbox (**frame = hint, API = truth**). Reconnect/backoff is owned by MO9b. On thread open the inbox
unread is invalidated (the thread read is marked module-side).

### 5. Mock parity (`EXPO_PUBLIC_MOCK_MODE=true`)
`core/mock/handlers/serviceRequests.handlers.ts` gains the four chat routes with a seeded owner↔provider thread on
**SR 101** (in progress, provider assigned = "Blue Marine Services") exercising the full surface: a **System** lifecycle
pill + owner/provider **text** + a provider **image** (`attachmentFileId` → the read-url mock returns a deterministic
`picsum` image) + an owner **location** (Port Hercule, Monaco). Send validates exactly-one-of and appends; the inbox
derives preview + unread from the thread. Image upload reuses the existing mocked `uploads/session|complete` (the raw PUT
is skipped under mock), so composing a photo works end-to-end offline.

### 6. i18n tr + en
`services.chat` was promoted from a bare string to an object (15 keys each in `en.json` + `tr.json`, verified parity;
the one legacy `t('services.chat')` usage repointed to `services.chat.label`). Keys: title/label/open/placeholder/empty/
sending/retry/closed/sharedLocation/openInMaps/imageUnavailable/imageError/imageUploadFailed/locationDenied/
locationUnavailable.

### Native config
- `npx expo install expo-location` (SDK 56 → `~56.0.23`; works in Expo Go).
- `app.json` — added the `expo-location` plugin with an iOS when-in-use usage string (for dev-client / production
  builds; Expo Go grants at runtime).

## Verification
- **(1) `tsc --noEmit`** → **0 errors**.
- **(2) i18n tr/en parity** → `services.chat` = 15 keys each, no mismatch; both locale files parse.
- **(2b) Metro bundle** (`expo export --platform ios`) → **success (exit 0)** — the full graph incl. `OwnerChatScreen`,
  `chatApi`/`useChat`, `locationService` (`expo-location`), and the mock handlers resolves and compiles.
- **(3) Cost-free** — grep of the chat UI/API/util files for amount/commission/margin/net/price/discount/economics/
  escrow/currency/invoice/balance → only false positives (`marginBottom`, `Location.Accuracy.Balanced`, the "never
  economics" doc line). No economics anywhere.
- **(7) MO1 stub** — `comingSoon`/`chatComingSoon` removed from the real detail screen; confirmed gone.

### Manual smoke (spec item 6 — running stack + Redis; to run on device/simulator)
Left for an on-device pass against the live BFF (the automated gates above cover build + types + parity + cost-free):
1. Open an SR detail → tap **Open chat** → thread renders text/image/location + System pill, own vs counterparty
   correct.
2. Send **text**, **image** (picker → M4f presigned upload, bytes not via BFF), **location** (permission prompt → pin) —
   each hits the exactly-one-of body shape; optimistic row → confirmed; kill the network to see the **Retry** row.
3. Tap an image → full-screen viewer; tap a location → native maps opens.
4. Have the provider send a message → the MO9b bell frame refetches the open thread + bumps the inbox unread.

## Next owner debt
**MO11 — CargoDry owner surface** (validate → activate → my-kits): the module already exposes the 3 participant
endpoints; the mobile BFF + FE need wiring plus the `cargodry-api` mobile-bff assertion whitelist.
