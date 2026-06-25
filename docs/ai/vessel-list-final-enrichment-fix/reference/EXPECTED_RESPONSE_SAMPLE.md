# Expected Final Vessel List Item Response

After the fix, each enriched vessel list item should be as close as possible to this shape:

```json
{
  "id": 20001,
  "vesselCode": "MOCK-BLUE-OCTOPUS",
  "name": "Blue Octopus",
  "slug": "blue-octopus",
  "vesselTypeCode": "MOTOR_YACHT",
  "flagCountryCode": "TR",
  "ownerUserId": 10003,
  "ownerProfileId": 11003,
  "ownerName": "Ayşe Demir",
  "ownerAvatarUrl": null,
  "lengthMeters": 13.8,
  "latitude": 40.9693,
  "longitude": 29.0523,
  "lastPositionDate": "2025-06-01T08:00:00Z",
  "lastLocationText": "Kalamış Marina",
  "operationalStatus": 1,
  "operationalStatusLabel": "In Service",
  "assetType": 1,
  "assetTypeLabel": "Motor Yacht",
  "ownershipStatus": 2,
  "ownershipStatusLabel": "Charter",
  "status": 2,
  "statusLabel": "Active",
  "thumbnailUrl": null,
  "isArchived": false,
  "createDate": "2026-06-15T12:14:43.578014Z"
}
```

`thumbnailUrl` may remain null if the current schema only has `FileId` and there are no local FileStorage demo objects. If so, document the gap and keep frontend fallback behavior.
