# Vessel Register Page Target Contract

The frontend route `/app/vessels/register` expects a backend bootstrap endpoint:

```http
GET /api/v1/admin-panel/vessels/register
```

This endpoint must return options required to render the register form. It must not create a vessel.

## GET /vessels/register — Bootstrap/options

Suggested response body:

```json
{
  "vesselRegister": {
    "defaults": {
      "flagCountryCode": "TR",
      "assetType": 1,
      "operationalStatus": 1,
      "ownershipStatus": 1,
      "isArchived": false
    },
    "options": {
      "vesselTypes": [
        { "code": "MOTOR_YACHT", "label": "Motor Yacht" }
      ],
      "assetTypes": [
        { "value": 1, "label": "Motor Yacht" }
      ],
      "operationalStatuses": [
        { "value": 1, "label": "In Service" }
      ],
      "ownershipStatuses": [
        { "value": 1, "label": "Private" }
      ],
      "flagCountries": [
        { "code": "TR", "label": "Türkiye" }
      ],
      "buildCountries": [
        { "code": "TR", "label": "Türkiye" }
      ],
      "hullMaterials": [
        { "code": "GRP", "label": "GRP" }
      ],
      "superstructureMaterials": [
        { "code": "ALUMINIUM", "label": "Aluminium" }
      ],
      "homePorts": [
        { "code": "BODRUM", "label": "Bodrum" }
      ],
      "ownerCandidates": [
        { "userId": 10003, "profileId": 11003, "displayName": "Ayşe Demir", "email": "ayse.demir@inktavia.local" }
      ]
    }
  },
  "warnings": []
}
```

Use existing DTO/envelope patterns. If frontend already expects a slightly different shape, adapt while preserving needed data.

## POST endpoint

If the register form submits to a missing route, implement one of these according to existing frontend/API convention:

```http
POST /api/v1/admin-panel/vessels/register
```

or existing:

```http
POST /api/v1/admin-panel/vessels
```

Do not create duplicate create endpoints if one already works. Add an alias only if necessary and documented.

## Data sources

Bootstrap options should come from:

1. Existing ReferenceData/Lookup endpoints, when available.
2. Existing hardcoded enum mappings in BFF only for closed numeric enums such as `AssetType`, `OperationalStatus`, `OwnershipStatus`, if ReferenceData does not own them yet.
3. Identity/Profile bulk user search/list for owner candidates, if available.
4. Safe fallback static local options only if modules are unavailable, with warnings.
