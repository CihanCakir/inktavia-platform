# Inktavia Marine OS - ServiceRequest Advanced MVP Realtime CQRS

Use this prompt with GitHub Copilot Agent inside the repository.

All generated implementation must be in English and must follow the existing Aizen/Inktavia architecture.

## Step 12 - Build validation and final report

Run validation commands according to the repository.

## Required commands

Try to run:

```bash
dotnet restore
dotnet build
```

If the repository has a specific solution file, build it explicitly.

If tests were added and the repository supports test execution:

```bash
dotnet test
```

If migrations are required and tooling is available, document or run the migration command according to the existing project pattern.

## Final report

Create:

```text
ai/service-request-advanced-mvp-realtime-cqrs/reports/12_FINAL_IMPLEMENTATION_REPORT.md
```

The final report must include:

- implemented projects and files
- created entities
- created DTOs/requests/enums
- created commands
- created queries
- created controllers/endpoints
- repository and DbContext summary
- migration status
- realtime integration details
- exact findings from `Core/Realtime/src/Aizen.Core.Realtime`
- group/event model summary
- cache implementation summary
- cross-module integration summary
- authorization/access rules
- build result
- test result if applicable
- unresolved gaps
- recommended next steps

## Acceptance criteria

The implementation is acceptable only if:

- ServiceRequest module compiles.
- All first-phase entities exist.
- CQRS commands and queries use typed returns.
- Controllers route to command/query handlers.
- Realtime integration reuses `Aizen.Core.Realtime`.
- Access validation is not skipped.
- FileStorage attachment references are modeled by `FileId`.
- Status history is written on lifecycle transitions.
- Cache invalidation exists for write operations that affect cached queries.
- Admin, owner, and provider flows are separated by authorization/resource checks.
- Final report is generated.
