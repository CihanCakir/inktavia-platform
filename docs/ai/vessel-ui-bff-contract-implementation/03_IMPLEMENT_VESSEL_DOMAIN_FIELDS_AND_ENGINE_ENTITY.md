# 03 — Implement Vessel Domain Fields and Engine Entity

Based on the audit, add only missing fields from `reference/VESSEL_MODULE_ENTITY_CHANGE_SPEC.md`.

Rules:

- Use existing base entity and audit conventions.
- Use private setters if existing entities do.
- Add domain methods/factory/update methods where required.
- Do not change existing public contracts unless needed for UI contract.
- Use project user id type; do not force Guid if the system uses int/long.
- Add XML/DocumentationInfo only if the repo convention requires it.

If `VesselEngine` already exists, extend it only if needed. If missing, create entity and EF config following project style.

Update report with every changed entity and skipped field.
