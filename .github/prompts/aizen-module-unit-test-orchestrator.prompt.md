---
description: Create incremental module-specific unit test prompts and unit test projects for Aizen modules.
mode: agent
---

# Aizen Module Unit Test Orchestrator

You are working in an Aizen framework-based .NET repository.

Read and execute:

```text
ai/aizen-module-unit-tests/all-in-one-orchestrator-prompt.md
```

Primary objective:

- Scan `Modules/*`.
- For every module, generate a module-specific prompt:
  - `Modules/{ModuleName}/docs/ai/{ModuleName}.unit-test.prompt.md`
- For every module, create/update:
  - `Modules/{ModuleName}/tests/UNIT_TEST_PROGRESS.md`
- Create unit test projects under each module's own `tests` directory:
  - API Controller tests
  - Application Command/Query Handler tests
  - Domain tests
- Make the process incremental:
  - Read the progress MD first.
  - Only add missing/new/changed tests.
  - Update the progress MD after every run.
- Preserve existing architecture and naming conventions.
