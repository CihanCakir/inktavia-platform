# 01 — Import and Classify Local Test Failures

Read every JSON file under:

```text
Bff/src/AdminPanel/docs/reports/LOCAL_2026-06-15T13-43-30-657Z/
```

Build a failure matrix with:

```text
suite
test name
method
path
request body
expected status
actual status
expected shape
actual body
failure category
recommended fix owner
```

Use `reference/LOCAL_TEST_REPORT_SUMMARY.md` and `reference/FAILURE_CLASSIFICATION_RULES.md`.

Generate:

```text
docs/reports/admin-panel-bff-local-failure-classification-report.md
```

Do not modify code until classification is complete.
