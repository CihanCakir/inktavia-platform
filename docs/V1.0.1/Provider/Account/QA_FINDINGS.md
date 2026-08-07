# Provider QA — Account (Profile / Settings / Support) (P-QA9)

## Static findings
- **Profile** (`/app/profile`) — implemented (api+hooks). Live-QA.
- **Settings** (`/app/settings`) — page exists but **no `api/` dir** → likely static/local-only. Verify it does real work
  (notification prefs? language? theme?) or reclassify/wire.
- **Support** (`/app/support`) — has `api/`, no `hooks/` dir → verify the live-support channel (N-D) is wired.

## Live walkthrough checklist
- [ ] Profile: loads real provider profile; edit + avatar persist; validation works.
- [ ] Settings: every control does something real (persists server-side or is clearly client-only); no dead toggles.
      Confirm whether notification preferences live here or under Notifications (avoid duplication).
- [ ] Support: opening a support request reaches the live-support channel (N-D) and an admin can see it; empty/loading states.
- [ ] No console errors; i18n tr+en complete on all three.

## Fix candidates
Settings wiring / dedup with Notifications prefs; Support channel verification → `FIX_*` here.
