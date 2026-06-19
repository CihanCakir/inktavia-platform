# 04 — Update EF Configuration and Migrations

Update Vessel DbContext/configurations for new fields/entities.

Tasks:

1. Add DbSet/configuration for VesselEngine if created.
2. Add column configuration for new fields.
3. Use nullable/defaults safely.
4. Generate migrations using existing migration project/context pattern.
5. Build after migration generation.

Do not create CargoDry/ServiceRequest migrations unless those modules are explicitly in scope and missing VesselId is verified.

Update `docs/reports/vessel-module-entity-and-migration-plan-report.md`.
