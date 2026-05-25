# Aizen Module Unit Test Prompt Pack

This pack creates a repeatable test-generation workflow for Aizen modules.

## What It Does

For each module under `Modules/{ModuleName}`, it creates:

```text
Modules/{ModuleName}/docs/ai/{ModuleName}.unit-test.prompt.md
Modules/{ModuleName}/tests/UNIT_TEST_PROGRESS.md
Modules/{ModuleName}/tests/Aizen.Modules.{ModuleName}.Api.UnitTests
Modules/{ModuleName}/tests/Aizen.Modules.{ModuleName}.Application.UnitTests
Modules/{ModuleName}/tests/Aizen.Modules.{ModuleName}.Domain.UnitTests
```

Only relevant test projects are created if the matching source projects exist.

## Main Idea

Each module owns its own unit test prompt and its own progress file.

When rerun, Copilot reads the progress file first and only adds tests for missing or changed files.

## Execution Order

1. Run the orchestrator prompt to scan modules and create module-specific prompts.
2. Run a specific module prompt when you want to add/update tests for that module.

## Recommended Commands

Copy all-in-one orchestrator prompt:

```bash
pbcopy < ai/aizen-module-unit-tests/all-in-one-orchestrator-prompt.md
```

Then paste into GitHub Copilot Agent.

After module prompts are generated, run a module-specific prompt:

```bash
pbcopy < Modules/Identity/docs/ai/Identity.unit-test.prompt.md
```

Then paste into GitHub Copilot Agent.
