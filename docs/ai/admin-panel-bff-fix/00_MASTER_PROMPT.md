# 00 - Master Prompt - AdminPanel BFF Corrective Completion

You are working inside the Inktavia Marine OS repository.

Perform a corrective completion pass for AdminPanel BFF under:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

The previous generation was partial. You must fix missing endpoints and wrong CQRS file organization.

## Key objectives

- Scan every active module `*Controller.cs` file.
- Add missing Identity authentication/authorization endpoints.
- Add missing Identity module endpoints relevant to AdminPanel.
- Add missing ReferenceData endpoints.
- Add missing relevant Vessel/FileStorage/ServiceRequest endpoints.
- Ensure all BFF Application command/query structures use separate files for command/query and handler.
- Ensure every internal call uses AizenRemoteCall.
- Ensure token forwarding with Keycloak bearer token and Identity token header.
- Regenerate AdminPanel BFF Postman docs.
- Run build validation.

Follow `RUN_THIS_FIRST_SINGLE_PROMPT.md` as the authoritative instruction set.
