# CI Status Checks (build + test)

## Objective
Provide deterministic CI checks that integrate with branch rulesets so that:
- PRs cannot be merged unless the solution builds successfully
- Tests run (or are explicitly skipped when none exist yet)

## Workflow Location
- `.github/workflows/ci.yml`

## Runtime & Stack
- .NET SDK: 9.0.x
- Runner: ubuntu-latest

## Jobs
### build
- `dotnet restore ./Aizen.sln`
- `dotnet build ./Aizen.sln --configuration Release --no-restore`

### test
- `dotnet test ./Aizen.sln --configuration Release --no-restore`
- If no test projects exist yet, the workflow skips tests without failing CI.

## Why job names matter
Rulesets require selecting the exact check names. This repo standardizes the required checks as:
- `build`
- `test`

## Ruleset integration
The protected branches (`main`, `develop`) require:
- status checks: build + test

This ensures:
- Merge is blocked when compilation fails
- Merge is blocked when tests fail
- Merge is allowed only after passing checks

## Evidence
See: `docs/01-git-governance/05-proof-log.md`
