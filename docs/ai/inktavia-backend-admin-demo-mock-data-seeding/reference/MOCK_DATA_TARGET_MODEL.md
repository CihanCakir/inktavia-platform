# Mock Data Target Model

## Purpose

Generate realistic demo/mock data for Admin Panel UI testing across:

- Identity
- Vessel
- ServiceRequest

The generated data must be understandable, navigable, and coherent across modules.

## Target outcome

After local/development startup seeding:

1. Admin Panel Identity screens show realistic users/profiles/statuses.
2. Admin Panel Vessel screens show realistic vessels connected to Identity users.
3. Admin Panel ServiceRequest screens show operational requests connected to vessels and Identity users.
4. Status/timeline/detail screens have enough data to test tables, filters, badges, drawers, modals, detail pages, and timeline components.

## Non-goals

- Do not generate production data.
- Do not generate Payment/Profile active data.
- Do not call BFF or internal APIs from seeders unless the repository already uses that pattern.
- Do not seed another module's database from the wrong service.
- Do not invent domain fields.

## Dataset size guidance

Use a dataset that is large enough for UI testing but not too large for local startup.

Recommended minimum:

```text
Identity:
  1-2 admin users
  8-12 customer/boat-owner-like users if supported
  5-8 provider/operator-like users if supported
  profiles across pending/approved/rejected/active/inactive states where supported

Vessel:
  12-20 vessels
  multiple vessel types/classes/statuses where supported
  ownership/user links to Identity UserIds
  technical profiles and documents where entities exist

ServiceRequest:
  25-40 requests
  spread across lifecycle statuses
  linked to real mock VesselIds and Identity UserIds
  offers/assignments/worklogs/messages/completion/dispute records where entities exist
```

Adjust counts based on real DbContext/entity constraints.

## Data style

Use realistic marine data:

- Turkish marinas and coastal locations where location fields exist
- vessel names suitable for yachts/boats
- plausible dimensions/tonnage/year/manufacturer data
- service categories such as maintenance, cleaning, winterization, engine service, electrical, hull inspection, moisture/odor control
- service request timelines and notes written in professional admin-test style
