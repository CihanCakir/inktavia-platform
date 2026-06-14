# Admin Web Testing Scenarios

Generate data that supports these Admin Panel testing flows.

## Identity screens

- View users/profiles list
- Filter by status/type/search
- Open profile detail
- Approve/reject pending profile where supported
- View user with roles/context where supported

## Vessel screens

- View vessels list
- Filter by status/type/location/owner
- Open vessel detail
- View owner information by UserId reference
- View technical information
- View documents/media placeholders if supported
- Test archive/restore/status update if endpoints exist

## ServiceRequest screens

- View service requests list
- Filter by status, vessel, owner, date, provider/assignment if supported
- Open request detail
- View status timeline
- View offers/assignments/worklogs/messages where supported
- Approve/reject completion if supported
- Review disputes if supported

## Cross-module scenarios

Data should make it easy to visually verify:

```text
User Ayşe Demir owns Blue Octopus.
Blue Octopus has an active service request.
The service request has status history, worklogs, and completion/dispute details.
Admin user can see all records through AdminPanel BFF.
```

## Negative scenarios

Include limited rejected/inactive/pending records so UI empty/error/state badges can be checked, but do not make the whole dataset invalid.
