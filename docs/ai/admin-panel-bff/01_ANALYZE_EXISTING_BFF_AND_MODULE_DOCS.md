# 01 - Analyze Existing BFF and Module Docs

## Goal

Analyze the existing Admin Panel BFF projects and active module endpoint documentation before creating code.

## Tasks

1. Inspect these projects:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application
```

2. Identify:

- `.csproj` names
- target framework
- existing controllers
- existing Program.cs / DI / middleware setup
- existing appsettings files
- existing Application layer CQRS conventions
- existing AizenRemoteCall references, if any

3. Inspect active module Postman docs:

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

If exact paths do not exist, search under each active module for:

```text
endpoint-inventory.md
postman-validation-report.md
*.postman_collection.json
```

4. Extract and document the endpoints relevant to Admin Panel BFF:

- Identity admin/profile/user endpoints
- ReferenceData lookup/tree/currency endpoints
- Vessel admin list/detail/status/document/media endpoints
- FileStorage metadata/signed-read/review endpoints
- ServiceRequest list/detail/dispute/completion/assignment/worklog/status endpoints

5. Inspect actual controller code when docs are incomplete:

```text
Modules/**/src/**/Controller/**/*.cs
Modules/**/src/**/Controllers/**/*.cs
```

6. Produce an analysis document:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/admin-panel-bff-analysis-report.md
```

## Report must include

- Existing BFF project state
- Active module endpoint inventory summary
- Candidate Admin BFF endpoints
- Request/response DTO source classes
- Required Abstraction project references
- Missing/blocked endpoints
- Payment/Profile skipped notes
- Risks and assumptions

Do not write implementation code until this report is generated.
