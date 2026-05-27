---
description: Run the module-specific unit test prompt for one target module.
mode: agent
---

# Run Aizen Module Unit Test Prompt

You are working in an Aizen framework-based .NET repository.

Target module is provided by the user as:

```text
[MODULE_NAME]
```

If the user did not provide the module name, ask for it.

Then read and execute:

```text
Modules/[MODULE_NAME]/docs/ai/[MODULE_NAME].unit-test.prompt.md
```

Rules:

- Start by reading `Modules/[MODULE_NAME]/tests/UNIT_TEST_PROGRESS.md`.
- Do not regenerate already completed tests unless the source file changed.
- Add tests only for uncovered or changed Controller, Command Handler, Query Handler, Domain Entity, Value Object, Domain Service, and Validator files.
- Update `UNIT_TEST_PROGRESS.md` at the end.
- Build and test only the affected test projects when possible.
