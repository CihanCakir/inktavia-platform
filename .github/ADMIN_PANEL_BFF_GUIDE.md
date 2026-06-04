# Admin Panel BFF Implementation Guide

## Goal

Generate an Admin Panel BFF for Inktavia Marine OS using the already-created projects:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

The Admin Panel BFF exposes public/admin-facing API endpoints and internally calls active module APIs through `AizenRemoteCall`.

## Active internal APIs

- Identity API
- ReferenceData API
- Vessel API
- FileStorage API
- ServiceRequest API

## Inactive/future APIs

- Payment API
- Profile API

## Data sources for endpoint discovery

The Agent must inspect:

```text
Modules/Identity/docs/postman/endpoint-inventory.md
Modules/Identity/docs/postman/postman-validation-report.md
Modules/ReferenceData/docs/postman/endpoint-inventory.md
Modules/ReferenceData/docs/postman/postman-validation-report.md
Modules/Vessel/docs/postman/endpoint-inventory.md
Modules/Vessel/docs/postman/postman-validation-report.md
Modules/FileStorage/docs/postman/endpoint-inventory.md
Modules/FileStorage/docs/postman/postman-validation-report.md
Modules/ServiceRequest/docs/postman/endpoint-inventory.md
Modules/ServiceRequest/docs/postman/postman-validation-report.md
```

If paths differ, search for `endpoint-inventory.md` and `postman-validation-report.md` under the active module directories.

## Admin BFF expected capabilities

- Admin dashboard summary
- ServiceRequest monitoring
- ServiceRequest detail aggregation
- ServiceRequest dispute management
- ServiceRequest completion approval queue
- Vessel list/detail for admin
- File review / attachment metadata / signed read URL orchestration
- ReferenceData lookup/tree support for admin screens
- Identity admin profile/user operations if active endpoints exist
- Health/diagnostic endpoints for active internal API reachability

## Implementation style

Application layer must be organized by service and sub-service:

```text
Aizen.Bff.AdminPanel.Application/
  Identity/
    UserProfiles/
      Query/
      Command/
      Service/
  ReferenceData/
    Lookups/
      Query/
      Service/
  Vessel/
    Vessels/
      Query/
      Service/
  FileStorage/
    Files/
      Query/
      Command/
      Service/
  ServiceRequest/
    Requests/
      Query/
      Command/
      Service/
    Disputes/
      Query/
      Command/
      Service/
    Completions/
      Query/
      Command/
      Service/
  Dashboard/
    Query/
    Service/
```

Controllers must remain thin and delegate to Application commands/queries.

---

# v2 - Cross-module AdminPanel scenarios

This package now includes two mandatory scenario prompts:

```text
ai/admin-panel-bff/09_CROSS_MODULE_ADMIN_SCENARIOS.md
ai/admin-panel-bff/10_CROSS_MODULE_ADMIN_COMMAND_SCENARIOS.md
```

## Query orchestration

The AdminPanel BFF must provide aggregate read models for admin screens instead of forcing the web client to call multiple internal module APIs.

Examples:

```text
Admin User Overview = Identity + Vessel + ServiceRequest + ReferenceData
Admin Vessel Overview = Vessel + Identity + ReferenceData + ServiceRequest
Admin Vessel Documents = Vessel + FileStorage + ReferenceData
Admin ServiceRequest Operation Detail = ServiceRequest + Vessel + Identity + FileStorage + ReferenceData
```

## Command orchestration

The AdminPanel BFF may expose admin command endpoints that orchestrate active module commands, but it must not own business rules.

Examples:

```text
Approve organizer profile = Identity command wrapper + optional refreshed profile response
Update vessel status = Vessel command wrapper + optional ServiceRequest warning enrichment
Resolve dispute = ServiceRequest command wrapper + optional operation detail refresh
Generate document read URL = Vessel document lookup + FileStorage signed URL command
```

If an active internal endpoint is missing, document the scenario as blocked or future. Do not fake internal capabilities in BFF.
