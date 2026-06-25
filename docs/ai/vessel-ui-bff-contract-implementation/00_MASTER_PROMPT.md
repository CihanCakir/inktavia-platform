# 00 — Master Prompt

Implement the Vessel UI backend contract across Vessel Module and AdminPanel BFF.

Primary goals:

1. Reconcile source contract with current repo architecture.
2. Implement MVP GET endpoints first.
3. Add verified missing Vessel entity/application fields.
4. Keep CargoDry and ServiceRequest joins optional/null-safe.
5. Preserve existing BFF auth/envelope/RemoteCall patterns.
6. Generate reports.

Execution order:

1. Source docs and route reconciliation.
2. Vessel audit.
3. Domain/entity work.
4. EF/migrations.
5. Application query/command work.
6. BFF DTO/RemoteCall/controller work.
7. Cross-module optional joins.
8. Build/smoke tests.
9. Reports.

Stop and report instead of guessing if a required entity or contract cannot be found.
