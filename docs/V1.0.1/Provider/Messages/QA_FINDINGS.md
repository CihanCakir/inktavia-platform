# Provider QA — Messages (P-QA4)

## Static findings
Implemented (`messages` api+hooks, page + `/:threadId`) — gated two-pane chat, realtime (framework), unread, attachments,
location, lifecycle system messages. Needs live-QA (realtime is best verified on-screen).

## Live walkthrough checklist (localhost:3002/app/messages)
- [ ] Conversation list loads; unread counts correct + clear on open.
- [ ] Realtime: a new inbound message appears live (no refresh); admin messages render distinctly (gold "Yönetici" bubble).
- [ ] Send text; attachment upload + render; location share.
- [ ] System/lifecycle messages appear (offer accepted, job started, etc.).
- [ ] Deep-link `/app/messages/:threadId` auto-selects the thread; no console errors.
