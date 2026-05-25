---
description: Diagnose and fix Npgsql DateTime Kind=Local to timestamp with time zone error in IdentityDbContext SaveChanges.
mode: agent
---

# Aizen Identity PostgreSQL DateTime UTC Fix

You are working in an Aizen framework-based .NET repository.

Read and execute:

```text
ai/aizen-identity-postgresql-datetime-utc-fix/all-in-one-agent-prompt.md
```

Primary objective:

- Diagnose the `Cannot write DateTime with Kind=Local to PostgreSQL type 'timestamp with time zone'` error.
- The error occurs during `AizenCommandHandlerDecorator -> unitOfWork.SaveChangesAsync()`.
- The first observed command is `ChangePasswordCommandHandler`.
- Identity persistence uses `Aizen.Modules.Identity.Repository.Context.IdentityDbContext`.
- Find the exact entity/property causing the error.
- Inspect base entity audit fields, token entities, repositories, DbContext mappings, and token creation code.
- Implement a clean UTC DateTime strategy across the affected Identity persistence path.
- Preserve the existing Aizen architecture.
