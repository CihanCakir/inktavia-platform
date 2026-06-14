# Identity Mock Data Specification

## Objective

Generate Identity mock data that provides real users/profiles for Admin Panel testing and stable UserIds for Vessel and ServiceRequest references.

## Required audit

Before generating mock JSON, inspect:

```text
Aizen.Modules.Identity.Domain
Aizen.Modules.Identity.Repository
Aizen.Modules.Identity.Application
Aizen.Modules.Identity.Abstraction
Identity DbContext
Identity entities
Profile entities
Role/profile/context entities
Existing seeders
Existing password hashing conventions
Existing login request/response DTOs
```

Do not create incompatible password values if Identity uses hashing. Use existing password hashing/seeding patterns.

## Suggested users

Create realistic local-development users using the real entity shape:

```text
admin users:
  admin@inktavia.local
  operations.admin@inktavia.local

boat-owner/customer-like users:
  ayse.demir@inktavia.local
  mehmet.kaya@inktavia.local
  deniz.yilmaz@inktavia.local
  selin.uzun@inktavia.local

provider/operator-like users if supported:
  marina.ops@inktavia.local
  teknik.servis@inktavia.local
  cargodry.team@inktavia.local
```

Use stable UserIds and document them in `admin-demo-id-manifest.json`.

## Profile/context requirements

If Identity supports panel/profile contexts, create records that allow:

- AdminPanel user to login and access AdminPanel BFF
- boat owner/customer users referenced by vessels and service requests
- provider/operator users referenced by offers/assignments/worklogs only if supported

Use actual enum names and profile types from the codebase. Do not invent new enum values.

## Status coverage

Where supported, include records across states:

```text
Active
Pending approval
Approved
Rejected
Inactive
```

Use actual status enum values.

## Security

- Local mock passwords are acceptable only in local/development JSON.
- Do not enable mock users in staging/production by default.
- Do not commit real user data.
- Do not create production-like admin secrets.
