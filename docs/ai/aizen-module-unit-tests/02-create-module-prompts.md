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
