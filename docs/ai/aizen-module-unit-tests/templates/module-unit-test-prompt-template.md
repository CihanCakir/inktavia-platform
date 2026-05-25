# [MODULE_NAME] Unit Test Prompt

## Target Module

```text
Modules/[MODULE_NAME]
```

## Goal

Create or update unit tests for `[MODULE_NAME]` incrementally.

Do not regenerate everything on every run.

Start by reading:

```text
Modules/[MODULE_NAME]/tests/UNIT_TEST_PROGRESS.md
```

If it does not exist, create it using the standard structure.

## Source Projects

Detect actual paths before making changes.

Expected source project candidates:

```text
Modules/[MODULE_NAME]/src/Aizen.Modules.[MODULE_NAME]/Aizen.Modules.[MODULE_NAME].csproj
Modules/[MODULE_NAME]/src/Aizen.Modules.[MODULE_NAME].Application/Aizen.Modules.[MODULE_NAME].Application.csproj
Modules/[MODULE_NAME]/src/Aizen.Modules.[MODULE_NAME].Domain/Aizen.Modules.[MODULE_NAME].Domain.csproj
Modules/[MODULE_NAME]/src/Aizen.Modules.[MODULE_NAME].Repository/Aizen.Modules.[MODULE_NAME].Repository.csproj
```

## Test Projects

Create if missing and source project exists:

```text
Modules/[MODULE_NAME]/tests/Aizen.Modules.[MODULE_NAME].Api.UnitTests
Modules/[MODULE_NAME]/tests/Aizen.Modules.[MODULE_NAME].Application.UnitTests
Modules/[MODULE_NAME]/tests/Aizen.Modules.[MODULE_NAME].Domain.UnitTests
```

## Test Scope

### API Controller Tests

Scan:

```text
Modules/[MODULE_NAME]/src/Aizen.Modules.[MODULE_NAME]/Controller/**/*.cs
Modules/[MODULE_NAME]/src/Aizen.Modules.[MODULE_NAME]/Controllers/**/*.cs
```

Create tests under:

```text
Modules/[MODULE_NAME]/tests/Aizen.Modules.[MODULE_NAME].Api.UnitTests/Controller
```

### Application Tests

Scan:

```text
Modules/[MODULE_NAME]/src/Aizen.Modules.[MODULE_NAME].Application/**/Command/**/*Handler.cs
Modules/[MODULE_NAME]/src/Aizen.Modules.[MODULE_NAME].Application/**/Query/**/*Handler.cs
Modules/[MODULE_NAME]/src/Aizen.Modules.[MODULE_NAME].Application/**/*Validator.cs
```

Create tests under:

```text
Modules/[MODULE_NAME]/tests/Aizen.Modules.[MODULE_NAME].Application.UnitTests
```

### Domain Tests

Scan:

```text
Modules/[MODULE_NAME]/src/Aizen.Modules.[MODULE_NAME].Domain/**/*.cs
```

Create tests under:

```text
Modules/[MODULE_NAME]/tests/Aizen.Modules.[MODULE_NAME].Domain.UnitTests
```

## Incremental Process

1. Read `UNIT_TEST_PROGRESS.md`.
2. Check git changes for this module:

```bash
git status --short Modules/[MODULE_NAME]
git diff --name-only HEAD -- Modules/[MODULE_NAME]
git diff --name-only origin/main...HEAD -- Modules/[MODULE_NAME]
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

Update `Modules/[MODULE_NAME]/tests/UNIT_TEST_PROGRESS.md` and then report:

```md
# [MODULE_NAME] Unit Test Run Report

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
