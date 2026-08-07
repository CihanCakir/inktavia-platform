# FIX — message thread doesn't scroll to bottom on open (stays at top)

> **Repo:** `inktavia-marine-provider-web` — FE only, tiny, additive. On `/app/messages/:threadId` the conversation opens
> scrolled to the **top**; the user must manually scroll down to the latest message. A chat should open at the **bottom**
> (most recent). **Do not commit** until reviewed.

## Root cause (found)
`MessagesPage.tsx` renders the thread messages in a scrollable container (line ~233:
`<div className="flex-1 space-y-3 overflow-y-auto p-4">`) and maps `items` (~line 239) — but there is **no
scroll-to-bottom logic**: no ref on the container, no `scrollIntoView`, no effect. So the pane stays at its default top
scroll position on load.

## Fix
1. Put a `ref` on the scrollable messages container (the `overflow-y-auto` div at ~233), e.g. `scrollRef`.
2. Add an effect that scrolls it to the bottom **when the thread's messages first render and on thread switch / new
   messages**:
   - On **initial load / thread change** → jump instantly (`el.scrollTop = el.scrollHeight`, `behavior: 'auto'`), keyed on
     `threadId` + the loaded `items` (e.g. depend on `threadId` and `items.length`), so it fires once the query resolves and
     the messages are painted.
   - On a **new incoming/sent message while already near the bottom** → smooth-scroll to bottom; if the user has scrolled up
     to read history, don't yank them down (optional refinement — check `scrollHeight - scrollTop - clientHeight` before
     auto-scrolling).
3. Handle async height growth: the initial scroll must run **after** the messages are in the DOM (the effect on `items`
   covers this). If attachments/images can change height after load, also scroll on image `onLoad` (or a short
   `requestAnimationFrame`/timeout after the messages render) so the first paint lands at the true bottom.

Prefer `container.scrollTop = container.scrollHeight` on the ref (reliable in a flex/overflow layout) over a bottom-sentinel
`scrollIntoView` (which can be janky on mount). Keep the existing optimistic-send/reconcile logic untouched — just ensure a
sent message also scrolls to bottom.

## Verify (on screen — localhost:3002/app/messages/9011)
- [ ] Opening a thread lands at the **latest** message (bottom), not the top — including a thread with many messages.
- [ ] Switching threads re-scrolls to the new thread's bottom.
- [ ] Sending a message scrolls to show it; a new incoming message scrolls to bottom when already at bottom.
- [ ] Scrolling up to read history is not fought by auto-scroll (if the refinement is added).
- [ ] No console errors; no layout jump/flicker.

## Report
`docs/V1.0.1/Provider/Messages/REPORT_FIX_THREAD_AUTOSCROLL.md`: the ref + effect added, the initial-load + thread-switch +
new-message behaviors, and the on-screen check.
