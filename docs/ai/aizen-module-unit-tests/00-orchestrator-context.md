# 00 - Orchestrator Context

## Goal

Create a reusable unit test generation system for all modules under:

```text
Modules/
```

Each module owns:

```text
src/
tests/
docs/ai/
```

The prompt must detect module structure and create unit tests under that module's `tests` directory.

## Example Module Structure

```text
Modules/Identity/
  src/
    Aizen.Modules.Identity/
    Aizen.Modules.Identity.Abstraction/
    Aizen.Modules.Identity.Application/
    Aizen.Modules.Identity.Core/
    Aizen.Modules.Identity.Domain/
    Aizen.Modules.Identity.Repository/
  tests/
```

## Required Per Module

For module `{ModuleName}` create/update:

```text
Modules/{ModuleName}/docs/ai/{ModuleName}.unit-test.prompt.md
Modules/{ModuleName}/tests/UNIT_TEST_PROGRESS.md
```

And create relevant test projects:

```text
Modules/{ModuleName}/tests/Aizen.Modules.{ModuleName}.Api.UnitTests
Modules/{ModuleName}/tests/Aizen.Modules.{ModuleName}.Application.UnitTests
Modules/{ModuleName}/tests/Aizen.Modules.{ModuleName}.Domain.UnitTests
```

Create only if relevant source project exists.

## Test Framework

If existing repository test conventions exist, follow them.

Otherwise use:

```text
xUnit
FluentAssertions
```

Prefer hand-written fakes/test doubles.

Use Moq or NSubstitute only if already used by the repository or unavoidable.
