# 05 — Generate ServiceRequest Mock JSON and Seeder

Generate ServiceRequest mock data linked to Identity mock UserIds and Vessel mock VesselIds.

## JSON files

Place files according to existing convention. If no stronger convention exists, use:

```text
Aizen.Modules.ServiceRequest.Repository/Seed/MockData/admin-demo/service-requests.json
Aizen.Modules.ServiceRequest.Repository/Seed/MockData/admin-demo/service-request-items.json
Aizen.Modules.ServiceRequest.Repository/Seed/MockData/admin-demo/service-request-offers.json
Aizen.Modules.ServiceRequest.Repository/Seed/MockData/admin-demo/service-request-assignments.json
Aizen.Modules.ServiceRequest.Repository/Seed/MockData/admin-demo/service-request-worklogs.json
Aizen.Modules.ServiceRequest.Repository/Seed/MockData/admin-demo/service-request-status-history.json
Aizen.Modules.ServiceRequest.Repository/Seed/MockData/admin-demo/service-request-messages.json
Aizen.Modules.ServiceRequest.Repository/Seed/MockData/admin-demo/service-request-completions.json
Aizen.Modules.ServiceRequest.Repository/Seed/MockData/admin-demo/service-request-disputes.json
Aizen.Modules.ServiceRequest.Repository/Seed/MockData/admin-demo/service-request-attachments.json
```

Only create files that map to real entities.

## Seeder

Create module-owned seed/import code such as:

```text
Aizen.Modules.ServiceRequest.Repository/Seed/MockData/ServiceRequestMockDataSeeder.cs
```

Adapt to existing DI/startup conventions.

## Rules

- Use Identity UserIds from manifest.
- Use VesselIds from manifest.
- Use actual ServiceRequest status enum names/values.
- Create lifecycle coverage for Admin Panel UI.
- Insert missing records only.
- Do not seed Vessel or Identity DB from ServiceRequest.
- Do not seed FileStorage DB from ServiceRequest.

## Report

Create:

```text
docs/reports/service-request-mock-data-seeding-report.md
```
