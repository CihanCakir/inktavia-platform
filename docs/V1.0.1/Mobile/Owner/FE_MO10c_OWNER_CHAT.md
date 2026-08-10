# FE_MO10c — owner chat screen (text + image + location) + realtime (Expo)

> **Repo:** `inktavia-marine-mobile` (RN/Expo). MO10 **phase c** per `MO10_PLAN.md` — the **close-out**: the FE
> consumes MO10a (inbox + thread + text send) and MO10b (image + location send, image read-url), with live updates via
> the MO9b bell. **Removes the MO1 "chat coming-soon" stub** — the last MO1 owner stub. Additive; tr+en; mock parity;
> cost-free. **Do not commit.**

## Backend contracts (already built — MO10a/b)
- **Inbox:** `GET api/v1/mobile/conversations` → `MobileConversationDto[]` (SR id/code, last-message preview, unread,
  updated-at; counterparty name is a known gap on the inbox — resolve/show it in the thread).
- **Thread:** `GET api/v1/mobile/service-requests/{srId}/messages` → `{ CounterpartyName, Items:
  MobileChatMessageDto[] }` where `MobileChatMessageDto = { Id, SenderType, IsOwn, MessageType(string), Content,
  AttachmentFileId, LocationLat/Lng/Label, IsRead, ReadAt, CreatedAt }`. **System** messages carried through for
  lifecycle pills.
- **Send:** `POST api/v1/mobile/service-requests/{srId}/messages` — body is **exactly one of** text (`Content`), image
  (`AttachmentFileId`), or location (`LocationLat/Lng/Label`). The BFF validates exactly-one + coord range + length.
- **Image upload:** reuse the **M4f client-presigned session** (the vessel-media upload the app already has) → the
  bytes go straight to storage, never through the BFF → you get a `fileId` → send with `AttachmentFileId`.
- **Image display:** `GET api/v1/mobile/service-requests/{srId}/attachments/{fileId}/read-url` → a short-TTL presigned
  GET url (owner-gated server-side).
- **Realtime:** the MO9b `/hubs/notification` bell already fires on `NewMessageReceived` (a `mobileNotification`
  frame). Use it as the live hint → refetch the open thread / bump the inbox.

## FE — build on `features/service-requests/` + `features/notifications/`
1. **API + hooks** (`chatApi.ts` + `useChat.ts`): inbox, thread (paged), send (text/image/location), attachment
   read-url, via the app's `normalizeEnvelope` + React Query. Remove any demo/hardcoded chat data; keep a realistic
   `EXPO_PUBLIC_MOCK_MODE` branch (a few owner↔provider mock messages incl. one image + one location).
2. **Remove the MO1 stub:** delete the "chat coming-soon" placeholder on the SR detail and route to the real chat.
3. **Chat screen** (opened from SR detail; optionally a conversations inbox list too):
   - **Message list** (inverted/auto-scroll, mirror the provider web chat): own vs counterparty bubbles (`IsOwn`),
     timestamps, read ticks (`IsRead`/`ReadAt`); **System** messages rendered as centered lifecycle **pills** (offer
     accepted, completion, etc.).
   - **Text** rendering.
   - **Image** message: thumbnail (lazy `read-url` per message, cached) → tap → full-screen viewer; upload progress +
     failed-send retry.
   - **Location** message: a **card** with a static map preview + address/label → tap → **open in the native maps app**
     (client-side nav, per the messaging decision).
   - **Composer:** text input + **attach image** (`expo-image-picker` → M4f presigned upload → send `AttachmentFileId`)
     + **share location** (`expo-location`, permission-guarded → send `LocationLat/Lng/Label`). Optimistic append +
     rollback on failure. Enforce exactly-one-of client-side too.
4. **Realtime:** subscribe to the MO9b bell; on a `mobileNotification` frame for `NewMessageReceived` on the open SR →
   invalidate/refetch the thread (frame = hint, API = truth); bump the inbox unread. Reconnect handled by MO9b.
5. **Unread/read:** show unread counts on the inbox; mark the thread read on open (existing module behaviour) and
   reflect read ticks.
6. **i18n tr+en**; mock parity; **cost-free** (chat shows only message text + image + location — never economics).

## Don't-break / QA
- Additive: `features/` gains the real chat API + screen + composer + realtime wiring; the MO1 stub is removed.
  Nothing else changes. Mock parity behind `EXPO_PUBLIC_MOCK_MODE`.
- Expo Go guard: image picker + location work on device; guard native calls so the app degrades gracefully in Expo Go
  (like the push + social patterns).
- **Cost-free:** grep the chat UI — no amount/commission/net/margin anywhere.
- Verification: (1) `tsc --noEmit` 0 errors; (2) i18n tr/en parity for all new keys; (3) thread renders text/image/
  location + System pills, own vs counterparty correct (`IsOwn`); (4) send text/image/location each hits the right
  body shape (exactly-one-of); image upload uses the M4f presigned session (bytes not via BFF); (5) image thumbnails
  resolve via read-url + open in viewer; location opens native maps; (6) a live new-message frame refetches the open
  thread + bumps unread (manual smoke on the running stack + Redis — document it); (7) the MO1 chat stub is gone.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_FE_MO10c_OWNER_CHAT.md`: the chat screen (text/image/location render + composer,
System pills, unread/read, auto-scroll), the image upload (M4f presigned) + display (read-url), the location share +
open-in-maps, the realtime new-message wiring (MO9b bell), the MO1 stub removal, and the tr+en + mock parity +
cost-free confirmation. **This closes MO10 — owner ↔ provider chat is at parity with the provider side (text + image +
location), the last MO1 stub is gone, and the owner side of the Messaging capability is complete.** Next owner debt:
**MO11 — CargoDry owner surface** (validate → activate → my-kits; the module has the 3 participant endpoints, the
mobile BFF + FE just need wiring + the `cargodry-api` mobile-bff assertion whitelist).
