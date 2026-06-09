# 03 — Generate Response Types and TypeScript Recommendations

Create:

```text
docs/admin-web-client/admin-panel-bff-response-types.md
```

For each response type:

- C# type name
- Namespace
- File path
- Endpoint usages
- JSON example
- TypeScript interface recommendation
- Nested DTOs
- Enum values if inferable
- Nullable fields
- Warning notes where inference is incomplete

Do not invent fields. Use actual DTO/response classes. If an endpoint has no explicit response type, mark it as `Needs verification`.
