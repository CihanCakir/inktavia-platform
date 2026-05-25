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
