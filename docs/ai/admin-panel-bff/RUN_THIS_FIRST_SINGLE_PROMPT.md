# RUN THIS FIRST - AdminPanel BFF v2

You are working inside the Inktavia Marine OS backend repository.

The AdminPanel BFF target projects already exist:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

Complete the AdminPanel BFF implementation using the prompt files in this directory.

## Required execution order

Execute these prompts in order:

```text
00_MASTER_PROMPT.md
01_ANALYZE_EXISTING_BFF_AND_MODULE_DOCS.md
02_REMOTE_CALL_AUTH_AND_APPSETTINGS.md
03_APPLICATION_LAYER_COMMANDS_QUERIES.md
04_ADMIN_DASHBOARD_AND_IDENTITY.md
05_SERVICEREQUEST_ADMIN_OPERATIONS.md
06_VESSEL_FILESTORAGE_REFERENCEDATA.md
07_CONTROLLERS_POSTMAN_AND_DOCS.md
08_BUILD_VALIDATION_AND_FINAL_REPORT.md
09_CROSS_MODULE_ADMIN_SCENARIOS.md
10_CROSS_MODULE_ADMIN_COMMAND_SCENARIOS.md
```

## Non-negotiable project rules

- Use the existing BFF projects. Do not create a parallel BFF architecture.
- Application layer must be organized by service/sub-service-based Command and Query structure.
- Use `AizenRemoteCall` for all internal service calls.
- Use each active module's Abstraction class libraries where available.
- Read each active module's `docs/postman/endpoint-inventory.md` and `postman-validation-report.md` before implementing BFF endpoints.
- Active modules: Identity, ReferenceData, Vessel, FileStorage, ServiceRequest.
- Inactive modules: Payment, Profile. Skip them or document as future placeholders.
- Forward both auth contexts to every internal API call:
  - `Authorization: Bearer {current Keycloak token}`
  - `X-Aizen-User-Token: {current Identity user token}`
- Controllers must be thin.
- Controllers must call Application commands/queries.
- Controllers must not call `AizenRemoteCall` directly.
- Do not put domain business logic into BFF.
- Do not directly access internal module databases.
- Every public class/interface/controller/command/query/handler/DTO must include `DocumentationInfo` following the repository convention.
- Every endpoint must return typed DTOs. Do not use `object` return types.

## v2 scope

This package must generate not only basic AdminPanel BFF proxy/orchestration endpoints, but also explicit cross-module AdminPanel scenarios.

### Cross-module query scenarios

Implement aggregate read endpoints such as:

```text
GET /api/v1/admin-panel/dashboard/overview
GET /api/v1/admin-panel/users/{userId}/overview
GET /api/v1/admin-panel/vessels/{vesselId}/overview
GET /api/v1/admin-panel/vessels/{vesselId}/documents
GET /api/v1/admin-panel/service-requests/{serviceRequestId}/operation-detail
GET /api/v1/admin-panel/service-requests/{serviceRequestId}/timeline
GET /api/v1/admin-panel/service-requests/filter-options
GET /api/v1/admin-panel/vessels/form-options
GET /api/v1/admin-panel/files/{fileId}/review-overview
```

### Cross-module command scenarios

Implement command/orchestration endpoints only when backed by active module endpoints, such as:

```text
POST /api/v1/admin-panel/users/{userId}/organizer-profiles/{profileId}/approve
POST /api/v1/admin-panel/users/{userId}/organizer-profiles/{profileId}/reject
POST /api/v1/admin-panel/users/{userId}/venue-profiles/{profileId}/approve
POST /api/v1/admin-panel/users/{userId}/venue-profiles/{profileId}/reject
POST /api/v1/admin-panel/vessels/{vesselId}/status
POST /api/v1/admin-panel/vessels/{vesselId}/archive
POST /api/v1/admin-panel/vessels/{vesselId}/restore
POST /api/v1/admin-panel/vessels/{vesselId}/ownerships
PUT /api/v1/admin-panel/vessels/{vesselId}/ownerships/{ownershipId}
DELETE /api/v1/admin-panel/vessels/{vesselId}/ownerships/{ownershipId}
POST /api/v1/admin-panel/vessels/{vesselId}/documents/{documentId}/read-url
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/status
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/cancel
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/assignments
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/assignments/{assignmentId}/reassign
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/completion/{completionId}/approve
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/completion/{completionId}/reject
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/disputes/{disputeId}/status
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/disputes/{disputeId}/resolve
```

If a required internal endpoint is missing, do not fake the command. Document it as `Blocked` or `Future` in the scenario matrix.

## Required outputs

Generate/update:

```text
Bff/src/AdminPanel/docs/postman/admin-panel-bff-endpoint-inventory.md
Bff/src/AdminPanel/docs/postman/admin-panel-bff-cross-module-endpoint-inventory.md
Bff/src/AdminPanel/docs/postman/admin-panel-bff-cross-module-command-inventory.md
Bff/src/AdminPanel/docs/postman/AdminPanelBff.ControllerApiTests.postman_collection.json
Bff/src/AdminPanel/docs/postman/AdminPanelBff.CrossModuleQueries.postman_collection.json
Bff/src/AdminPanel/docs/postman/AdminPanelBff.CrossModuleCommands.postman_collection.json
Bff/src/AdminPanel/docs/postman/admin-panel-bff-validation-report.md
Bff/src/AdminPanel/docs/admin-panel-bff-final-report.md
Bff/src/AdminPanel/docs/admin-panel-bff-cross-module-query-scenarios-final-report.md
Bff/src/AdminPanel/docs/admin-panel-bff-cross-module-command-final-report.md
```

Run:

```bash
dotnet restore
dotnet build
```

Fix only AdminPanel BFF related compilation issues unless another change is absolutely required and documented.
