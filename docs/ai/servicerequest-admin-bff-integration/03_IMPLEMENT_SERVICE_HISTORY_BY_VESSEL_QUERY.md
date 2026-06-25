# 03 — Implement Service History by Vessel Query

Implement or complete a ServiceRequest module query for Vessel Detail service history.

## Target internal module behavior

Expose a ServiceRequest admin query/controller route that can return the latest service request history for a vessel.

Preferred route if no existing equivalent exists:

```http
GET /api/v1/admin/service-requests/by-vessel/{vesselId}/history?take=10
```

If an existing list endpoint with `vesselId` filter already exists and is canonical, extend it instead and document the route used.

## Required response fields

Each item must expose:
- `Id`
- `Date`
- `ServiceType`
- `Provider`
- `Location`
- `Notes`
- `Status`

## Date mapping

Use this preference order:
1. completion/resolution date if available
2. scheduled/requested service date if available
3. create date

## Provider/location/notes mapping

Use existing ServiceRequest model fields. If a field cannot be derived safely, return null and document it.

## Output

Update ServiceRequest abstraction/application/controller as needed. Keep CQRS pattern.
