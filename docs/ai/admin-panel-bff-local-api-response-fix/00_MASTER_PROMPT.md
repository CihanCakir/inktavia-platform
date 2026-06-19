# 00 — Master Prompt

You are fixing AdminPanel BFF using the local BFF API response reports.

Target projects:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

Source reports:

```text
Bff/src/AdminPanel/docs/reports/LOCAL_2026-06-15T13-43-30-657Z/
```

Primary goal:

- make all active AdminPanel BFF endpoints behave correctly,
- fix authentication scheme failures,
- add missing active endpoints,
- align or feature-gate future/inactive endpoints,
- update the test runner/Postman collection to match the final Identity-only browser auth model.

Do not invent internal module business logic. Use real active module contracts and BFF orchestration patterns.
