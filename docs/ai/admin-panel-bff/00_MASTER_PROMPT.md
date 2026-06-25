# 00 - MASTER PROMPT - Admin Panel BFF

You are working inside the Inktavia Marine OS backend repository.

The Admin Panel BFF target projects have already been created:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

Your task is to complete the Admin Panel BFF implementation in a controlled, architecture-compliant way.

## Critical project context

Active internal modules:

- Identity
- ReferenceData
- Vessel
- FileStorage
- ServiceRequest

Inactive/future modules:

- Payment
- Profile

Do not generate active flows for Payment or Profile. If you discover references to them, mark them as future integration / skipped.

## Required BFF behavior

The Admin Panel BFF must:

1. Expose admin-focused API endpoints.
2. Use `AizenRemoteCall` for all calls to internal module APIs.
3. Forward the incoming Keycloak bearer token to internal APIs.
4. Forward the incoming Identity user token through `X-Aizen-User-Token`.
5. Use active modules' Abstraction class libraries as references when available.
6. Build its own screen/API-specific Admin BFF DTOs only when aggregation or shaping is needed.
7. Avoid domain business logic.
8. Avoid direct database access.
9. Avoid direct repository usage from internal modules.
10. Generate docs/postman outputs and final validation reports.

## Existing auth model

Incoming Admin Panel BFF requests use:

```text
Authorization: Bearer {{active_access_token}}
X-Aizen-User-Token: {{X-Aizen-User-Token}}
```

When the BFF calls internal services using `AizenRemoteCall`, it must forward both headers.

## Local internal service defaults

Use these defaults in development appsettings:

```text
Identity:        http://localhost:7101/api/v1
ReferenceData:  http://localhost:7104/api/v1
Vessel:         http://localhost:7105/api/v1
FileStorage:    http://localhost:7106/api/v1
ServiceRequest: http://localhost:7107/api/v1
```

## Required output files

Generate or update:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/appsettings.Development.json
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/postman/endpoint-inventory.md
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/postman/AdminPanelBff.ControllerApiTests.postman_collection.json
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/postman/AdminPanelBff.BusinessScenarios.postman_collection.json
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/postman/postman-validation-report.md
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/admin-panel-bff-implementation-report.md
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/admin-panel-bff-final-validation-report.md
```

Also update project references, dependency injection, controllers, Application commands/queries/services, and appsettings as needed.

## Required implementation phases

Proceed in the following order:

1. Analyze existing BFF projects and active module docs/postman files.
2. Inspect the existing `AizenRemoteCall` usage in the repository.
3. Implement remote service configuration and auth header forwarding.
4. Create Application layer commands/queries/services by service and sub-service.
5. Create Admin Dashboard and Identity-related BFF endpoints where active internal endpoints exist.
6. Create ServiceRequest admin operations endpoints.
7. Create Vessel, FileStorage, and ReferenceData admin endpoints.
8. Generate Admin BFF Postman collections and docs.
9. Run restore/build and produce final validation report.

## Do not proceed blindly

Before writing code, produce a short analysis report that lists:

- discovered BFF project structure
- discovered active module endpoint inventories
- discovered available ServiceRequest admin operations
- discovered `AizenRemoteCall` patterns
- missing/blocked endpoints
- inactive Payment/Profile references to skip

Then implement only what can be grounded in existing controllers/contracts/docs.

---

# v2 extension - Cross-module AdminPanel scenarios

After completing the baseline AdminPanel BFF structure, you must also execute these additional scenario prompts:

```text
09_CROSS_MODULE_ADMIN_SCENARIOS.md
10_CROSS_MODULE_ADMIN_COMMAND_SCENARIOS.md
```

These are mandatory in v2.

## Cross-module query scenarios

Create AdminPanel-specific aggregate query endpoints by orchestrating active internal modules through `AizenRemoteCall`:

- Admin Dashboard Overview
- Admin User Overview
- Admin Vessel Overview
- Admin Vessel Documents
- Admin ServiceRequest Operation Detail
- Admin ServiceRequest Timeline
- Admin ServiceRequest Filter Options
- Admin Vessel Form Options
- Admin File Review Overview

## Cross-module command scenarios

Create AdminPanel-specific command/orchestration endpoints only when backed by active internal module endpoints:

- Identity/admin approval and rejection actions
- Vessel status, archive, restore and ownership actions
- Vessel/FileStorage signed URL and document operations
- ServiceRequest admin status, assignment, offer, completion and dispute actions
- Bulk/operational admin actions where internal APIs support them

Do not invent missing internal module endpoints. If a scenario is blocked, document it in the scenario matrix and final report.
