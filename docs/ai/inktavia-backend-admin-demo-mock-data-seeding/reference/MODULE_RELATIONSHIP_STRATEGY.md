# Module Relationship Strategy

## Core rule

Each service owns its own database. Cross-module relationships must use stable IDs, not cross-database writes.

## Identity as user authority

Identity owns:

```text
UserId
User profile/context
User status
Identity access/refresh token logic
Panel/domain authorization
```

Vessel and ServiceRequest must reference Identity users using `UserId` fields that exist in their own entities.

## Required cross-module ID manifest

Create a manifest if no existing one exists:

```text
docs/mock-data/admin-demo/admin-demo-id-manifest.json
```

Example shape:

```json
{
  "dataset": "admin-demo",
  "identity": {
    "users": {
      "admin.cihan": "11111111-1111-1111-1111-111111111111",
      "owner.ayse": "22222222-2222-2222-2222-222222222222"
    }
  },
  "vessel": {
    "vessels": {
      "blue-octopus": "33333333-3333-3333-3333-333333333333"
    }
  },
  "serviceRequest": {
    "requests": {
      "sr-winterization-blue-octopus": "44444444-4444-4444-4444-444444444444"
    }
  }
}
```

Use actual ID types used by the entities. If a module uses `long`, `int`, `Guid`, `string`, or custom `AizenId`, adapt to the real type.

## User relationship examples

Vessel module should reference Identity users by fields such as:

```text
OwnerUserId
CreatedByUserId
UpdatedByUserId
PrimaryContactUserId
```

Only use fields that exist in the actual Vessel entities.

ServiceRequest module should reference Identity users by fields such as:

```text
RequesterUserId
BoatOwnerUserId
ProviderUserId
AssignedUserId
CreatedByUserId
MessageSenderUserId
WorkLogCreatedByUserId
```

Only use fields that exist in the actual ServiceRequest entities.

## Vessel relationship examples

ServiceRequest should link to Vessel using real fields such as:

```text
VesselId
VesselSnapshot
VesselNameSnapshot
```

Only use fields that exist in the actual ServiceRequest entities.

## Idempotency

Every seed should be deterministic:

- Use stable IDs.
- Insert missing records only.
- Do not duplicate data on repeated startup.
- If an existing record with the same stable ID exists, skip or update only when the repository already has a safe seed update convention.
