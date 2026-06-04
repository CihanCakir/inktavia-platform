# 07 - Controllers, Postman, and Docs

## Goal

Create thin Admin Panel BFF controllers and generate matching Postman documentation.

## Controller rules

Controllers must:

- live under the existing API project
- follow existing controller route/versioning conventions
- use Application commands/queries/services
- not contain remote-call orchestration directly unless this is the existing repository pattern
- not contain domain business logic
- use typed request/response contracts
- include DocumentationInfo if required

## Suggested controller grouping

Adapt names to existing conventions:

```text
AdminDashboardController
AdminIdentityController
AdminServiceRequestController
AdminVesselController
AdminFileStorageController
AdminReferenceDataController
```

Route prefix suggestion:

```text
/api/v1/admin-panel/...
```

If the repository uses a different versioning or route pattern, follow the existing pattern.

## Postman outputs

Generate:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/postman/endpoint-inventory.md
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/postman/AdminPanelBff.ControllerApiTests.postman_collection.json
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/postman/AdminPanelBff.BusinessScenarios.postman_collection.json
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/postman/postman-validation-report.md
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/postman/admin-panel-bff-testing-guide.md
```

## Postman auth standard

Every Admin BFF request must use:

```text
Authorization: Bearer {{active_access_token}}
X-Aizen-User-Token: {{X-Aizen-User-Token}}
```

## Controller API collection

Group requests by controller:

```text
Admin Dashboard
Admin Identity
Admin ServiceRequest
Admin Vessel
Admin FileStorage
Admin ReferenceData
```

Every request must contain:

- URL using `{{admin_panel_bff_base_url}}`
- auth bearer token using `{{active_access_token}}`
- `X-Aizen-User-Token` header
- valid request body according to BFF request DTO
- test scripts for status code and response shape
- variable extraction where relevant

## Business scenario collection

Create scenario folders:

```text
00 - Auth Context Validation
01 - Admin Dashboard
02 - ServiceRequest Operations
03 - Completion Approval Queue
04 - Dispute Management
05 - Vessel Review
06 - File Review
07 - ReferenceData Support
08 - Health and Diagnostics
```

## Environment variables

Create or update environment examples with:

```text
admin_panel_bff_root_url=http://localhost:<ADMIN_BFF_PORT>
admin_panel_bff_base_url={{admin_panel_bff_root_url}}/api/v1
active_access_token=
X-Aizen-User-Token=
serviceRequestId=
vesselId=
fileId=
disputeId=
completionId=
```

If the Admin BFF port is unknown, use a placeholder and document it.
