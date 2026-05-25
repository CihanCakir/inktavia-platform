# All-in-One Orchestrator Prompt - Aizen Module Unit Test System

Execute this task as a coding agent.

Your task is to create a repeatable and incremental unit test generation system for every module under the `Modules` directory.

Key requirements:

- Every module must own its own unit test prompt.
- Every module must own its own `tests/UNIT_TEST_PROGRESS.md`.
- Unit test projects must be created inside the module's own `tests` directory.
- API Controller tests, Application Command/Query Handler tests, Validator tests, and Domain tests must be generated separately.
- Later runs must read the progress MD first and only add missing/new/changed tests.
- Do not rewrite the whole module every time.

Follow every section in order.



---

# 00-orchestrator-context.md

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


---

# 01-discover-modules.md

# 01 - Discover Modules

Do not create tests yet.

Scan:

```text
Modules/*
```

A valid module is a directory containing:

```text
src/
```

Optionally:

```text
tests/
docs/
```

For each module, identify:

- Module folder name, for example `Identity`
- API project:
  - `Modules/{Module}/src/Aizen.Modules.{Module}/Aizen.Modules.{Module}.csproj`
- Application project:
  - `Modules/{Module}/src/Aizen.Modules.{Module}.Application/Aizen.Modules.{Module}.Application.csproj`
- Domain project:
  - `Modules/{Module}/src/Aizen.Modules.{Module}.Domain/Aizen.Modules.{Module}.Domain.csproj`
- Repository project:
  - `Modules/{Module}/src/Aizen.Modules.{Module}.Repository/Aizen.Modules.{Module}.Repository.csproj`
- Abstraction project:
  - `Modules/{Module}/src/Aizen.Modules.{Module}.Abstraction/Aizen.Modules.{Module}.Abstraction.csproj`

Use actual detected paths, not assumptions only.

## Required Output

Produce:

```md
# Module Discovery Report

| Module | API Project | Application Project | Domain Project | Tests Directory |
|---|---|---|---|---|
| ... | ... | ... | ... | ... |
```


---

# 02-create-module-prompts.md

# 02 - Create Module-Specific Unit Test Prompts

For every discovered module, create:

```text
Modules/{ModuleName}/docs/ai/{ModuleName}.unit-test.prompt.md
```

The module-specific prompt must be generated from the template in:

```text
ai/aizen-module-unit-tests/templates/module-unit-test-prompt-template.md
```

Replace:

```text
[MODULE_NAME]
```

with the actual module name.

The prompt must instruct Copilot to:

1. Read `Modules/{ModuleName}/tests/UNIT_TEST_PROGRESS.md`.
2. Inspect only the target module.
3. Detect Controller, Command Handler, Query Handler, Validator, Domain Entity, Value Object, Domain Service files.
4. Create/update tests under the module's own `tests` directory.
5. Update progress MD after completion.
6. Build/test only affected module test projects where possible.

## Required Output

Produce:

```md
# Module Prompt Creation Report

| Module | Prompt File |
|---|---|
| Identity | Modules/Identity/docs/ai/Identity.unit-test.prompt.md |
```


---

# 03-create-progress-md.md

# 03 - Create or Update Unit Test Progress MD

For every discovered module, create if missing:

```text
Modules/{ModuleName}/tests/UNIT_TEST_PROGRESS.md
```

Do not overwrite if it already exists. Update only missing sections.

## Required Structure

Use this structure:

```md
# {ModuleName} Unit Test Progress

## Module

- Name: {ModuleName}
- Last Updated:
- Last Baseline Commit:
- Last Source Scan:

## Test Projects

| Layer | Project Path | Exists | Notes |
|---|---|---|---|
| API | tests/Aizen.Modules.{ModuleName}.Api.UnitTests | No | ... |
| Application | tests/Aizen.Modules.{ModuleName}.Application.UnitTests | No | ... |
| Domain | tests/Aizen.Modules.{ModuleName}.Domain.UnitTests | No | ... |

## Covered API Controllers

| Source File | Test File | Status | Last Updated |
|---|---|---|---|

## Covered Application Command Handlers

| Source File | Test File | Status | Last Updated |
|---|---|---|---|

## Covered Application Query Handlers

| Source File | Test File | Status | Last Updated |
|---|---|---|---|

## Covered Validators

| Source File | Test File | Status | Last Updated |
|---|---|---|---|

## Covered Domain Types

| Source File | Test File | Status | Last Updated |
|---|---|---|---|

## Pending Gaps

- ...

## Last Run Summary

- ...
```

## Incremental Rule

On later runs, the module-specific prompt must read this file first and skip already covered stable files.


---

# 04-create-test-projects.md

# 04 - Create Unit Test Projects Per Module

For every discovered module, create test projects only when corresponding source projects exist.

## API Test Project

If API project exists:

```text
Modules/{Module}/src/Aizen.Modules.{Module}/Aizen.Modules.{Module}.csproj
```

create:

```text
Modules/{Module}/tests/Aizen.Modules.{Module}.Api.UnitTests
```

## Application Test Project

If Application project exists:

```text
Modules/{Module}/src/Aizen.Modules.{Module}.Application/Aizen.Modules.{Module}.Application.csproj
```

create:

```text
Modules/{Module}/tests/Aizen.Modules.{Module}.Application.UnitTests
```

## Domain Test Project

If Domain project exists:

```text
Modules/{Module}/src/Aizen.Modules.{Module}.Domain/Aizen.Modules.{Module}.Domain.csproj
```

create:

```text
Modules/{Module}/tests/Aizen.Modules.{Module}.Domain.UnitTests
```

## Default Commands

Use detected paths. Example for Identity:

```bash
dotnet new xunit -n Aizen.Modules.Identity.Api.UnitTests -o Modules/Identity/tests/Aizen.Modules.Identity.Api.UnitTests
dotnet add Modules/Identity/tests/Aizen.Modules.Identity.Api.UnitTests/Aizen.Modules.Identity.Api.UnitTests.csproj reference Modules/Identity/src/Aizen.Modules.Identity/Aizen.Modules.Identity.csproj

dotnet new xunit -n Aizen.Modules.Identity.Application.UnitTests -o Modules/Identity/tests/Aizen.Modules.Identity.Application.UnitTests
dotnet add Modules/Identity/tests/Aizen.Modules.Identity.Application.UnitTests/Aizen.Modules.Identity.Application.UnitTests.csproj reference Modules/Identity/src/Aizen.Modules.Identity.Application/Aizen.Modules.Identity.Application.csproj

dotnet new xunit -n Aizen.Modules.Identity.Domain.UnitTests -o Modules/Identity/tests/Aizen.Modules.Identity.Domain.UnitTests
dotnet add Modules/Identity/tests/Aizen.Modules.Identity.Domain.UnitTests/Aizen.Modules.Identity.Domain.UnitTests.csproj reference Modules/Identity/src/Aizen.Modules.Identity.Domain/Aizen.Modules.Identity.Domain.csproj
```

## Packages

If repository has no test convention, add:

```bash
dotnet add {testProject} package FluentAssertions
```

If mocks are required and repository already uses a framework, use the existing one.

Prefer hand-written fakes/test doubles.

## Solution

If a solution file exists at root, add test projects:

```bash
dotnet sln Aizen.sln add {testProject}
```

Use the actual solution file if different.

## Do Not

Do not recreate existing test projects.

Do not overwrite custom test configuration.


---

# 05-test-generation-rules.md

# 05 - Test Generation Rules

## API Controller Tests

For source:

```text
Modules/{Module}/src/Aizen.Modules.{Module}/Controller/**/*.cs
Modules/{Module}/src/Aizen.Modules.{Module}/Controllers/**/*.cs
```

create tests under:

```text
Modules/{Module}/tests/Aizen.Modules.{Module}.Api.UnitTests/Controller/...
```

Test:

- route/action method behavior where possible
- validation behavior when controller has explicit model validation handling
- success response wrapping if AizenApiResponse is used
- authorization attributes only if they are present and testable without integration setup
- command/query dispatch calls through fake mediator/CQRS processor if used

Avoid full HTTP integration tests unless specifically requested.

## Application Command/Query Handler Tests

For source:

```text
Modules/{Module}/src/Aizen.Modules.{Module}.Application/**/Command/**/*Handler.cs
Modules/{Module}/src/Aizen.Modules.{Module}.Application/**/Query/**/*Handler.cs
```

create tests under:

```text
Modules/{Module}/tests/Aizen.Modules.{Module}.Application.UnitTests/Command/...
Modules/{Module}/tests/Aizen.Modules.{Module}.Application.UnitTests/Query/...
```

Test:

- happy path
- not found / validation / business exception path
- repository interaction through hand-written fakes where possible
- AizenBusinessException error code checks
- no null reference for InfoAccessor-driven user context
- cancellation token usage where meaningful

## Validators

If FluentValidation validators exist, create validator tests.

Test:

- required fields
- min/max length
- compare rules
- enum range
- invalid id values

## Domain Tests

For source:

```text
Modules/{Module}/src/Aizen.Modules.{Module}.Domain/**/*.cs
```

create tests under:

```text
Modules/{Module}/tests/Aizen.Modules.{Module}.Domain.UnitTests/...
```

Test:

- factory/create methods
- domain methods
- guard clauses
- state transitions
- entity invariants
- value object equality
- domain exceptions

## Naming

Use test class names:

```text
{SourceClassName}Tests
```

Use test method names:

```text
MethodName_Should_ExpectedBehavior_When_Condition
```

Example:

```csharp
ChangePassword_Should_ThrowUserNotFound_When_UserInfoIsMissing()
```

## Existing Architecture

Preserve current Aizen patterns.

Do not force a new testing framework if the repository already has one.


---

# 06-incremental-update-rules.md

# 06 - Incremental Update Rules

Every module-specific test generation run must be incremental.

## First Step

Read:

```text
Modules/{Module}/tests/UNIT_TEST_PROGRESS.md
```

If it does not exist, create it.

## Determine What To Test

Use these inputs:

1. `UNIT_TEST_PROGRESS.md`
2. Git changed files if available:

```bash
git status --short Modules/{Module}
git diff --name-only HEAD -- Modules/{Module}
git diff --name-only origin/main...HEAD -- Modules/{Module}
```

3. Source scan for uncovered files:
   - Controllers
   - Command Handlers
   - Query Handlers
   - Validators
   - Domain entities/value objects/domain services

## Skip Rules

Skip files if:

- They are already listed as `Completed`.
- Source file did not change since last baseline.
- Existing test file still exists.
- No meaningful behavior is present.

## Update Rules

Add or update tests if:

- Source file is new.
- Source file changed.
- Progress status is `Pending`, `Partial`, or `Needs Update`.
- Existing test file does not compile.
- Important behavior is uncovered.

## Progress MD Update

At the end, update:

```text
Modules/{Module}/tests/UNIT_TEST_PROGRESS.md
```

with:

- Last Updated
- Last Baseline Commit
- Last Source Scan
- Added tests
- Updated tests
- Pending gaps
- Build/test result

## Do Not

Do not reread and rewrite the entire module unnecessarily.

Do not duplicate tests for already covered stable files.


---

# 07-build-and-verify.md

# 07 - Build and Verify

After generating or updating tests for a module, run the smallest useful build/test command.

## Per Project

Example:

```bash
dotnet test Modules/{Module}/tests/Aizen.Modules.{Module}.Application.UnitTests/Aizen.Modules.{Module}.Application.UnitTests.csproj
```

## Per Module

If multiple test projects changed:

```bash
dotnet test Modules/{Module}/tests/Aizen.Modules.{Module}.Api.UnitTests/Aizen.Modules.{Module}.Api.UnitTests.csproj
dotnet test Modules/{Module}/tests/Aizen.Modules.{Module}.Application.UnitTests/Aizen.Modules.{Module}.Application.UnitTests.csproj
dotnet test Modules/{Module}/tests/Aizen.Modules.{Module}.Domain.UnitTests/Aizen.Modules.{Module}.Domain.UnitTests.csproj
```

## Full Solution

Run only if needed:

```bash
dotnet test
```

## Update Progress File

Write the result to:

```text
Modules/{Module}/tests/UNIT_TEST_PROGRESS.md
```

Include:

```md
## Last Run Summary

- Date:
- Added:
- Updated:
- Skipped:
- Build Result:
- Test Result:
- Remaining Issues:
```


---

# module-unit-test-prompt-template.md

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
