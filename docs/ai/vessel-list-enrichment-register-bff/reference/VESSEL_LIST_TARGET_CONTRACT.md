# Vessel List Target Contract

The Vessel list endpoint must return a UI-ready page where these table fields are populated when data exists.

Endpoint:

```http
GET /api/v1/admin-panel/vessels?pageIndex=0&pageSize=20&searchTerm=&assetTypes=&ownershipStatuses=&operationalStatuses=
```

Expected response body shape must preserve the existing BFF envelope and `AdminVesselListBffResponse` shape.

Required list item data:

```json
{
  "id": 20001,
  "vesselCode": "MOCK-BLUE-OCTOPUS",
  "name": "Blue Octopus",
  "slug": "blue-octopus",
  "vesselTypeCode": "MOTOR_YACHT",
  "flagCountryCode": "TR",
  "thumbnailUrl": null,
  "ownerName": "Ayşe Demir",
  "ownerAvatarUrl": null,
  "lengthMeters": 13.8,
  "grossTonnage": 18.5,
  "latitude": 40.9693,
  "longitude": 29.0523,
  "lastPositionDate": "2026-06-19T...Z",
  "lastLocationText": "Kalamış Marina",
  "operationalStatus": 1,
  "operationalStatusLabel": "In Service",
  "assetType": 1,
  "assetTypeLabel": "Motor Yacht",
  "ownershipStatus": 1,
  "ownershipStatusLabel": "Private",
  "status": 1,
  "statusLabel": "Active",
  "isArchived": false,
  "createDate": "..."
}
```

If existing DTOs do not have label/text fields, add them additively as nullable fields. Do not remove existing fields.

## Table mapping

- `Gemi`: `name`, `vesselCode`, `flagCountryCode`, `thumbnailUrl`
- `Sahip`: `ownerName`, optionally `ownerAvatarUrl`
- `Tür / Uzunluk`: `vesselTypeCode`, `lengthMeters`
- `Son Konum`: Prefer `lastLocationText`; fallback to `latitude, longitude`; include `lastPositionDate` if UI supports it
- `Durum`: Prefer `operationalStatusLabel`; fallback to `statusLabel`; retain numeric status fields for filters

## Enrichment policy

- Owner display information must be enriched by BFF using bulk Identity/Profile lookup.
- Location must come from the Vessel module list query/read model, preferably the latest `VesselLocationSnapshot`.
- Status labels can be mapped in BFF from numeric enums/codes or from ReferenceData if such lookup exists.
