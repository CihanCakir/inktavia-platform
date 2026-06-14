# Vessel Mock Data Specification

## Objective

Generate realistic Vessel mock data linked to Identity mock UserIds.

## Required audit

Before generating mock JSON, inspect:

```text
Aizen.Modules.Vessel.Domain
Aizen.Modules.Vessel.Repository
Aizen.Modules.Vessel.Application
Aizen.Modules.Vessel.Abstraction
VesselDbContext
Vessel entities
Ownership entities
Technical profile entities
Document/media entities
Location snapshot/value objects
Existing seeders
Enum definitions
Validation rules
```

Do not invent fields or enum values.

## Relationship requirement

Every vessel owner/contact reference must use a stable UserId from Identity mock data.

Examples only; adapt to real entity fields:

```text
OwnerUserId -> Identity mock UserId
CreatedByUserId -> admin mock UserId
UpdatedByUserId -> admin mock UserId
```

## Suggested vessel scenarios

Create vessels covering UI filters and detail pages:

```text
Blue Octopus — motor yacht, approved/active
Golden Tide — sailing yacht, pending document review
Marmara Pearl — cabin cruiser, active with service history
Aegean Wind — sailboat, archived/inactive if supported
CargoDry Demo Boat — has moisture/odor service requests
Kalamis Runner — maintenance-heavy vessel
Bodrum Star — premium yacht profile
Gocek Breeze — assigned service requests
```

## Data coverage

Where supported by entities, include:

```text
Vessel core profile
Ownership relation
Technical profile
Engine/fuel/material/type details
Location/marina snapshot
Documents linked by FileId-like placeholder values if FileStorage relationship exists
Status/history records where entities exist
Created/updated audit fields
```

If FileStorage FileIds are required and no FileStorage mock seed is in scope, use deterministic placeholder FileIds and document them as external references. Do not insert FileStorage DB records from Vessel.
