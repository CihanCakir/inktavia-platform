# 10 - Build Validation and Final Report

Run validation.

```text
dotnet restore
dotnet build
```

Fix only AdminPanel BFF related compile issues unless a very small shared reference fix is required.

Generate final report:

```text
Bff/src/AdminPanel/docs/admin-panel-bff-fix-final-report.md
```

The report must include:

- Files changed
- Controllers scanned
- Endpoint count by module
- Endpoints added to BFF
- Endpoints skipped with reason
- Identity auth endpoints status
- ReferenceData endpoint coverage status
- CQRS file split corrections
- Controller naming cleanup
- AizenRemoteCall usage verification
- Auth header forwarding verification
- Appsettings updates
- Postman files generated/updated
- Build status
- Remaining known issues
