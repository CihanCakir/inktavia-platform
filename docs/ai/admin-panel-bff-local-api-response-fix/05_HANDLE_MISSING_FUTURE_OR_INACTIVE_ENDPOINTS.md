# 05 — Handle Missing Future or Inactive Endpoints

Classify endpoints such as:

```text
/cargodry/kits
/notification-templates
/payments/transactions
/payments/commissions
/reports
/reports/kpi
/analytics/dashboard
```

If no real active module/controller contract exists:

1. Do not invent production business logic.
2. Add feature flags or explicit inactive endpoint responses where appropriate.
3. If Admin Web needs local visual testing, implement local/demo-only mock BFF endpoints guarded by config.
4. Update tests to expect the correct status for inactive endpoints, unless the endpoint is intentionally enabled for local demo.

Generate:

```text
docs/reports/admin-panel-bff-future-inactive-endpoint-decision-report.md
```
