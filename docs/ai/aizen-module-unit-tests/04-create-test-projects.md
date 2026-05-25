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
