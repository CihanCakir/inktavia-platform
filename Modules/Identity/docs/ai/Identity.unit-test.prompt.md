# Identity Unit Test Prompt

## Target Module

```text
Modules/Identity
```

## Goal

Create or update unit tests for `Identity` incrementally.

Do not regenerate everything on every run.

Start by reading:

```text
Modules/Identity/tests/UNIT_TEST_PROGRESS.md
```

If it does not exist, create it using the standard structure.

## Source Projects

Detect actual paths before making changes.

Expected source project candidates:

```text
Modules/Identity/src/Aizen.Modules.Identity/Aizen.Modules.Identity.csproj
Modules/Identity/src/Aizen.Modules.Identity.Application/Aizen.Modules.Identity.Application.csproj
Modules/Identity/src/Aizen.Modules.Identity.Domain/Aizen.Modules.Identity.Domain.csproj
Modules/Identity/src/Aizen.Modules.Identity.Repository/Aizen.Modules.Identity.Repository.csproj
```

## Test Projects

Create if missing and source project exists:

```text
Modules/Identity/tests/Aizen.Modules.Identity.Api.UnitTests
Modules/Identity/tests/Aizen.Modules.Identity.Application.UnitTests
Modules/Identity/tests/Aizen.Modules.Identity.Domain.UnitTests
```

## Test Scope

### API Controller Tests

Scan:

```text
Modules/Identity/src/Aizen.Modules.Identity/Controller/**/*.cs
Modules/Identity/src/Aizen.Modules.Identity/Controllers/**/*.cs
```

Create tests under:

```text
Modules/Identity/tests/Aizen.Modules.Identity.Api.UnitTests/Controller
```

### Application Tests

Scan:

```text
Modules/Identity/src/Aizen.Modules.Identity.Application/**/Command/**/*Handler.cs
Modules/Identity/src/Aizen.Modules.Identity.Application/**/Query/**/*Handler.cs
Modules/Identity/src/Aizen.Modules.Identity.Application/**/*Validator.cs
```

Create tests under:

```text
Modules/Identity/tests/Aizen.Modules.Identity.Application.UnitTests
```

### Domain Tests

Scan:

```text
Modules/Identity/src/Aizen.Modules.Identity.Domain/**/*.cs
```

Create tests under:

```text
Modules/Identity/tests/Aizen.Modules.Identity.Domain.UnitTests
```

## Incremental Process

1. Read `UNIT_TEST_PROGRESS.md`.
2. Check git changes for this module:

```bash
git status --short Modules/Identity
git diff --name-only HEAD -- Modules/Identity
git diff --name-only origin/main...HEAD -- Modules/Identity
```

3. Identify files marked as:
   - Pending
   - Partial
   - Needs Update
   - Not listed but discovered in source
4. Add/update tests only for those files.
5. Update `UNIT_TEST_PROGRESS.md`.

## Testing Style

Use existing repository test conventions.

If no convention exists:

- Use xUnit
- Use FluentAssertions
- Prefer hand-written fakes/test doubles
- Use Moq/NSubstitute only if repository already uses it or unavoidable

## Architecture Rules

- Preserve Aizen architecture.
- Do not create integration tests unless explicitly asked.
- Do not use real external services.
- Do not call real databases in unit tests.
- For Command/Query handlers, fake repositories/services.
- For domain tests, test pure behavior.
- For controllers, fake CQRS/MediatR/processor dependencies.

## Required Final Report

Update `Modules/Identity/tests/UNIT_TEST_PROGRESS.md` and then report:

```md
# Identity Unit Test Run Report

## Added Test Projects
- ...

## Added Tests
- ...

## Updated Tests
- ...

## Skipped Existing Coverage
- ...

## Pending Gaps
- ...

## Build/Test Result
- ...
```
