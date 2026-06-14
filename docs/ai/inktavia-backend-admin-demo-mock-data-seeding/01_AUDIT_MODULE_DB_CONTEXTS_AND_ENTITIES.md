# 01 — Audit Module DbContexts and Entities

Audit the real module data models before generating anything.

## Inspect Identity

Search and inspect:

```text
Aizen.Modules.Identity.Domain
Aizen.Modules.Identity.Repository
Aizen.Modules.Identity.Application
Aizen.Modules.Identity.Abstraction
*Identity*DbContext*.cs
*User*.cs
*Profile*.cs
*Role*.cs
*Context*.cs
*Seed*.cs
```

Document:

- DbContext name and project
- entities and required fields
- profile/user relationships
- supported status/profile enums
- password hashing/credential storage convention
- existing seed convention

## Inspect Vessel

Search and inspect:

```text
Aizen.Modules.Vessel.Domain
Aizen.Modules.Vessel.Repository
Aizen.Modules.Vessel.Application
Aizen.Modules.Vessel.Abstraction
*Vessel*DbContext*.cs
*Vessel*.cs
*Ownership*.cs
*Technical*.cs
*Document*.cs
*Media*.cs
*Seed*.cs
```

Document:

- DbContext name and project
- aggregate/entity relationships
- Identity UserId fields
- FileId/document references
- supported enums and statuses
- existing seed convention

## Inspect ServiceRequest

Search and inspect:

```text
Aizen.Modules.ServiceRequest.Domain
Aizen.Modules.ServiceRequest.Repository
Aizen.Modules.ServiceRequest.Application
Aizen.Modules.ServiceRequest.Abstraction
*ServiceRequest*DbContext*.cs
*Offer*.cs
*Assignment*.cs
*WorkLog*.cs
*Completion*.cs
*Dispute*.cs
*Message*.cs
*StatusHistory*.cs
*Attachment*.cs
*Seed*.cs
```

Document:

- DbContext name and project
- lifecycle entities
- Identity UserId fields
- VesselId fields
- FileId/attachment references
- supported statuses and enum values
- existing seed convention

## Report

Create:

```text
docs/reports/mock-data-module-entity-audit-report.md
```

Include exact file paths inspected and any uncertainty.
