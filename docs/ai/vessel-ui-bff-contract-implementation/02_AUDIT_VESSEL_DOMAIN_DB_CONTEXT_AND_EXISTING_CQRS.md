# 02 — Audit Vessel Domain, DbContext and CQRS

Inspect Vessel module projects:

- Domain entities
- DbContext and EF configurations
- Abstraction DTO/Response/Request classes
- Application command/query handlers
- existing admin controllers
- repository/services

Find exact names for:

- Vessel
- VesselSpecification / VesselSpec
- VesselOwnership
- VesselDocument
- VesselDocumentVersion
- VesselMedia
- VesselLocationSnapshot
- existing engine entity, if any

Output:

- existing fields
- missing fields
- existing queries/endpoints
- existing response shapes
- known Refit/System.Text.Json risks

Write findings to `docs/reports/vessel-module-entity-and-migration-plan-report.md`.
