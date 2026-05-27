# Aizen Module Unit Test System

## Purpose

This prompt pack creates a repeatable unit test generation workflow for every module under the `Modules` directory.

Each module has a structure similar to:

```text
Modules/
  Identity/
    src/
      Aizen.Modules.Identity/
      Aizen.Modules.Identity.Abstraction/
      Aizen.Modules.Identity.Application/
      Aizen.Modules.Identity.Core/
      Aizen.Modules.Identity.Domain/
      Aizen.Modules.Identity.Repository/
    tests/
```

The goal is to create unit test projects under each module's own `tests` directory and generate module-specific prompt files so unit tests can be added incrementally over time.

## Required Output Per Module

For each module, create/update:

```text
Modules/{ModuleName}/tests/
  Aizen.Modules.{ModuleName}.Api.UnitTests/
  Aizen.Modules.{ModuleName}.Application.UnitTests/
  Aizen.Modules.{ModuleName}.Domain.UnitTests/
  UNIT_TEST_PROGRESS.md

Modules/{ModuleName}/docs/ai/
  {ModuleName}.unit-test.prompt.md
```

Only create a test project if the corresponding source project exists.

Example for Identity:

```text
Modules/Identity/tests/Aizen.Modules.Identity.Api.UnitTests
Modules/Identity/tests/Aizen.Modules.Identity.Application.UnitTests
Modules/Identity/tests/Aizen.Modules.Identity.Domain.UnitTests
Modules/Identity/tests/UNIT_TEST_PROGRESS.md
Modules/Identity/docs/ai/Identity.unit-test.prompt.md
```

## Test Targets

### API / Controller Tests

Source location usually:

```text
Modules/{ModuleName}/src/Aizen.Modules.{ModuleName}/Controller/**/*.cs
Modules/{ModuleName}/src/Aizen.Modules.{ModuleName}/Controllers/**/*.cs
```

Test location:

```text
Modules/{ModuleName}/tests/Aizen.Modules.{ModuleName}.Api.UnitTests/Controller/...
```

### Application Tests

Command and Query Handler source location usually:

```text
Modules/{ModuleName}/src/Aizen.Modules.{ModuleName}.Application/**/Command/**/*Handler.cs
Modules/{ModuleName}/src/Aizen.Modules.{ModuleName}.Application/**/Query/**/*Handler.cs
```

Test location:

```text
Modules/{ModuleName}/tests/Aizen.Modules.{ModuleName}.Application.UnitTests/Command/...
Modules/{ModuleName}/tests/Aizen.Modules.{ModuleName}.Application.UnitTests/Query/...
```

### Domain Tests

Domain source location usually:

```text
Modules/{ModuleName}/src/Aizen.Modules.{ModuleName}.Domain/**/*.cs
```

Test location:

```text
Modules/{ModuleName}/tests/Aizen.Modules.{ModuleName}.Domain.UnitTests/...
```

## Incremental Behavior

Before generating tests, Copilot must read:

```text
Modules/{ModuleName}/tests/UNIT_TEST_PROGRESS.md
```

If it exists, use it as the source of truth for:

- Already created test projects
- Already covered controllers
- Already covered command handlers
- Already covered query handlers
- Already covered domain entities/value objects/domain services
- Last baseline commit/file list
- Pending test gaps

If the file does not exist, create it.

On every run:

1. Read `UNIT_TEST_PROGRESS.md`.
2. Detect new/changed source files since the last baseline if possible.
3. Add tests only for uncovered or changed files.
4. Update `UNIT_TEST_PROGRESS.md`.
5. Do not rewrite all tests unnecessarily.

## Important

Preserve the existing Aizen architecture.

Do not create integration tests unless explicitly requested.

Prefer unit tests.

If the project already has test framework conventions, use them.

If no convention exists, use:

- xUnit
- FluentAssertions
- Hand-written fakes/test doubles preferred
- Use Moq/NSubstitute only if already used in repository or unavoidable
