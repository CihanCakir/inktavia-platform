# 00 — Master Prompt: Admin Web Client Contract

Analyze the AdminPanel BFF and generate the React.js Admin Web client contract package.

Target projects:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

Create documentation under:

```text
docs/admin-web-client/
```

Core requirements:

- List every AdminPanel BFF endpoint.
- Explain the purpose of every endpoint.
- Extract request and response types.
- Generate frontend TypeScript type recommendations from actual DTOs.
- Document browser authentication with Keycloak Authorization Code + PKCE.
- Document Identity token handling and `X-Aizen-User-Token` forwarding.
- Document React.js admin client architecture.
- Do not call internal module APIs directly from React.
- Do not activate Payment or Profile flows.
