# 05 — React Admin Web Architecture

Create:

```text
docs/admin-web-client/admin-web-client-architecture.md
```

If a React project already exists, adapt to it.

If no project exists, propose a React + TypeScript + Vite architecture:

```text
apps/admin-panel-web/
  src/
    app/
    config/
    auth/
    api/
    features/
    layouts/
    components/
    hooks/
    types/
```

Include:

- Auth provider
- Protected route strategy
- API client structure
- Feature foldering
- Error/loading/empty state strategy
- DTO-to-view-model mapping
- Future realtime notification placeholder
- AdminPanel BFF-only data access rule
