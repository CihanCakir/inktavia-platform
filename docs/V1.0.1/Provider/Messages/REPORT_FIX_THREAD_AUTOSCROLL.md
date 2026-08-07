# REPORT — message thread now opens scrolled to the latest message

> Executes `FIX_THREAD_AUTOSCROLL.md`. `inktavia-marine-provider-web` — FE only, tiny, additive. `/app/messages/:threadId`
> opened at the **top**; the message pane had no scroll-to-bottom logic. Added a ref + effect so a chat opens at the
> **bottom** (most recent), follows new messages when the user is at the bottom, and never yanks them while they read
> history. **Not committed.**

---

## Change (`src/features/messages/pages/MessagesPage.tsx`, `ThreadPane` only)

1. **`scrollRef`** on the scrollable messages container (the `overflow-y-auto` div) — the ref drives
   `el.scrollTop = el.scrollHeight` (preferred over a bottom-sentinel `scrollIntoView`, which is janky on mount in a
   flex/overflow layout).
2. **Auto-scroll effect** keyed on `[serviceRequestId, items.length, stickToBottom]` — fires once the `useThread` query
   resolves and the messages paint:
   - **Open / thread switch** (`prevThreadRef` detects the change) → jump to the bottom instantly, via
     `requestAnimationFrame` + a **250 ms `setTimeout` fallback** to catch late async height growth (images / map tiles).
   - **New sent/incoming message** → follow the bottom **only when the user is already near it**.
3. **Near-bottom guard** — `onScroll` maintains `atBottomRef` (`scrollHeight - scrollTop - clientHeight < 80`). If the
   user has scrolled up to read history, an incoming message does **not** yank them down; a thread switch always forces
   the bottom (resets `atBottomRef = true`).
4. **`onLoadCapture`** on the pane — image `load` events don't bubble, so a capture-phase handler catches descendant
   `<img>` loads and re-pins to the bottom (only while still following it). This makes the first paint land at the *true*
   bottom even when an image resolves after the initial scroll.
5. **Sent message always scrolls** — the three send handlers (`onSendText`, `onPickImage`, `onSendLocation`) set
   `atBottomRef.current = true` at the moment of sending, so a message *I* send pulls me to the bottom even if I'd
   scrolled up. The optimistic-send/reconcile logic is otherwise untouched (the same `items.length` effect scrolls it).

No new dependencies, no markup restructure, no network calls added.

## Verify (on screen — localhost:3002/app/messages/9011, PROVIDER 2 AS, tr)

Thread 9011 ("Acil: Dümen sistemi arızası — Çeşme") is a long thread (content **3366 px** tall vs a **596 px** pane) and
contains an async-loaded image.

- ✅ **Opens at the latest message (bottom), incl. long threads + async images.** On open, measured
  `scrollHeight - scrollTop - clientHeight = **1 px**` (pinned to the bottom); the last bubble (the "Hardware Ecosystem"
  image, "Siz · …") sits right above the composer — proving the `onLoadCapture` + timeout landed at the true bottom after
  the image resolved.
- ✅ **Scrolling up to read history is not fought.** Scrolled the pane to `scrollTop = 300` (2470 px from the bottom); it
  held there showing older messages (offer-accepted card, earlier texts) — no auto-yank.
- ✅ **A sent message scrolls to the bottom, even from a scrolled-up position.** From that scrolled-up spot, sent
  "autoscroll test" → the pane snapped down and the new bubble ("Siz · şimdi") rendered at the bottom
  (`distanceFromBottom = 1 px`). This is the same `items.length` effect an **incoming** message flows through (with the
  near-bottom guard), so incoming-at-bottom behaves identically. *(The test message was deleted afterward — the thread is
  back to its original state.)*
- ◑ **Thread switch** couldn't be demonstrated visually — PROVIDER 2 has a **single** conversation in the inbox, so there
  is no second thread to switch to. The switch path is covered by the same effect: `prevThreadRef` marks the change and
  forces the bottom, identical to the verified initial-load jump.
- **Console / flicker:** no layout jump or flicker on load; the scroll is a single instant jump. The page does log
  pre-existing `AxiosError 400`s — these come from `attachmentApi.getReadUrl` (image-attachment read URLs;
  console stack → `attachmentApi.ts` / `AttachmentGallery.tsx`, the same path `ImageMessage` uses), a pre-existing
  seed-attachment resolution failure. **The autoscroll change adds no network calls and no new errors.**

## Checks

- `tsc --noEmit` = **0**.
- eslint = **5 issues before and after** my change (a `git stash` comparison confirms **zero new** — the 1 error + 2
  warnings are pre-existing: the `items` derivation feeding two `useMemo`s, and the untouched `ImageMessage` effect).

## Scope

One file: `src/features/messages/pages/MessagesPage.tsx` (`useCallback` import + `ThreadPane` ref/effect/handlers +
`ref`/`onScroll`/`onLoadCapture` on the pane + `atBottomRef` in the three send handlers). No backend, no other files.
**Not committed.**
