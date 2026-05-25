# Aizen Identity PostgreSQL DateTime UTC Fix Prompt Pack

This prompt pack guides GitHub Copilot Agent to diagnose and fix an Npgsql DateTime UTC issue.

## Error

```text
Cannot write DateTime with Kind=Local to PostgreSQL type 'timestamp with time zone', only UTC is supported.
```

## Context

The error occurs in:

```csharp
await unitOfWork.SaveChangesAsync();
```

inside:

```text
AizenCommandHandlerDecorator
```

The first observed command is:

```text
ChangePasswordCommandHandler
```

The Identity module uses:

```text
Aizen.Modules.Identity.Repository.Context.IdentityDbContext
```

## Execution Order

1. `00-context-and-error.md`
2. `01-discovery-savechanges-and-unitofwork.md`
3. `02-discovery-identity-dbcontext-and-mappings.md`
4. `03-discovery-entities-and-datetime-fields.md`
5. `04-discovery-change-password-flow.md`
6. `05-discovery-token-and-audit-date-generation.md`
7. `06-root-cause-isolation.md`
8. `07-utc-strategy-design.md`
9. `08-implement-utc-fix.md`
10. `09-tests-and-verification.md`
11. `10-final-report.md`

For one-shot execution, use:

```text
all-in-one-agent-prompt.md
```
