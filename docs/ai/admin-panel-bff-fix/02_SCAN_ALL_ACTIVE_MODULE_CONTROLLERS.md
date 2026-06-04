# 02 - Scan All Active Module Controllers

Exhaustively scan active module controllers directly from source code.

Active modules:

```text
Identity
ReferenceData
Vessel
FileStorage
ServiceRequest
```

Scan patterns:

```text
Modules/Identity/src/**/*Controller.cs
Modules/ReferenceData/src/**/*Controller.cs
Modules/Vessel/src/**/*Controller.cs
Modules/FileStorage/src/**/*Controller.cs
Modules/ServiceRequest/src/**/*Controller.cs
Modules/**/src/**/Controller/**/*.cs
Modules/**/src/**/Controllers/**/*.cs
```

For each endpoint, extract:

- Module
- Controller
- Action
- HTTP method
- Route
- Request DTO
- Response DTO / return type
- Route/query parameters
- Authorization attributes
- Related command/query
- AdminPanel relevance

Do not scan Payment/Profile as active modules.

Generate:

```text
Bff/src/AdminPanel/docs/postman/source-controller-endpoint-audit.md
```
