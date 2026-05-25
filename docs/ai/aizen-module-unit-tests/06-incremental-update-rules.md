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
