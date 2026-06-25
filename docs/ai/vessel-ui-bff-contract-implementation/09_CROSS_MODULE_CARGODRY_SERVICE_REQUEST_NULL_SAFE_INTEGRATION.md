# 09 — Cross-Module CargoDry and ServiceRequest Null-Safe Integration

Audit CargoDry and ServiceRequest modules.

For Vessel detail response:

- Query CargoDry kits by vessel id if a safe contract exists.
- Query service history by vessel id if a safe contract exists.
- If no contract exists, return empty arrays and document missing endpoint/field.

Do not add direct database joins across modules.

If CargoDry/ServiceRequest lack VesselId, report migration need but do not implement unless the user explicitly wants that module in this task scope.

Write `docs/reports/vessel-cross-module-integration-report.md`.
