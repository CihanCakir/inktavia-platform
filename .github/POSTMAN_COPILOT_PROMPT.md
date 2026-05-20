# Copilot Prompt: Generate and Sync Postman Collection

Read this file first:

```text
docs/ai/postman/00_AGENT_ENTRYPOINT.md
```

Then implement the full Postman setup according to all referenced documents.

Required outputs:

```text
infrastructure/postman/inktavia-local.postman_environment.json
infrastructure/postman/inktavia-keycloak-api-tests.postman_collection.json
docs/postman/POSTMAN_LOCAL_TESTING.md
tools/postman/sync-postman-collection.mjs
```

Controller endpoint source:

```text
src/MDYKE/**/Controller/V1/**/*Controller.cs
src/MDYKE/**/Controllers/V1/**/*Controller.cs
```

After implementation, run or make available this command:

```bash
node tools/postman/sync-postman-collection.mjs
```

If `package.json` exists, also add:

```json
{
  "scripts": {
    "postman:sync": "node tools/postman/sync-postman-collection.mjs"
  }
}
```

Important:

- Use existing Keycloak values from the realm setup.
- Do not invent another auth model.
- Use application clients for login:
  - inktavia-mobile
  - customer-panel
  - admin-panel
- Use API clients as audiences:
  - identity-api
  - profile-api
  - payment-api
- Use test users:
  - mobile.user@inktavia.com
  - customer.user@inktavia.com
  - admin.user@inktavia.com
- Preserve ASP.NET Identity / EF Identity business ownership.
- Postman only tests token/API access; Identity module still manages local user/profile/domain behavior.
