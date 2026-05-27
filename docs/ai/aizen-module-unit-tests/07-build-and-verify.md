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
