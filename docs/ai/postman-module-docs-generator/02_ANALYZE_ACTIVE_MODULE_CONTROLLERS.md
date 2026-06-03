# 02 - Analyze Active Module Controllers

Scan active modules only:

- Identity
- ReferenceData
- Vessel
- FileStorage
- ServiceRequest

Exclude:

- Payment
- Profile

For every endpoint, extract:

- Module
- Controller
- Action
- HTTP method
- Full route
- Request DTO
- Response DTO
- Route params
- Query params
- Body/form params
- Authorization
- Source path
- Command/query mapping if inferable

Create endpoint inventories under every module:

```text
Modules/<ModuleName>/docs/postman/endpoint-inventory.md
```

Also create root summary:

```text
docs/postman/active-module-endpoint-inventory.md
```
