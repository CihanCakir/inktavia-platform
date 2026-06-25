# Target ServiceRequest AdminPanel BFF Endpoint Contract

Use the existing AdminPanel BFF route convention. In this repository, routes are expected under:

```text
/api/v1/admin-panel/...
```

Do not create a parallel `/bff/...` route unless existing controllers already use it.

## MVP read endpoints

### 1. GET `/api/v1/admin-panel/service-requests`

Admin service request list with filters.

Query parameters:
- `index` / `pageIndex` depending on existing convention
- `size` / `pageSize` depending on existing convention
- `search`
- `vesselId`
- `ownerUserId`
- `providerId`
- `statuses[]`
- `serviceTypes[]`
- `priorities[]`
- `dateFrom`
- `dateTo`

Response body:

```json
{
  "serviceRequests": {
    "from": 0,
    "index": 0,
    "size": 20,
    "count": 10,
    "pages": 1,
    "hasPrevious": false,
    "hasNext": false,
    "items": [
      {
        "id": "sr-uuid",
        "requestCode": "SR-2026-0001",
        "vesselId": 1,
        "vesselName": "Serenity IV",
        "ownerName": "Ahmet Yılmaz",
        "providerId": "provider-uuid",
        "providerName": "Bodrum Marine Service",
        "serviceType": "Annual Survey",
        "categoryName": "Survey",
        "status": "completed",
        "priority": "normal",
        "location": "Bodrum Marina",
        "notes": "Annual certificate renewal",
        "createdAt": "2026-06-19T12:00:00Z",
        "requestedDate": "2026-06-21T09:00:00Z",
        "lastActivityAt": "2026-06-21T15:30:00Z"
      }
    ]
  },
  "warnings": []
}
```

### 2. GET `/api/v1/admin-panel/service-requests/{id}`

Admin detail aggregate.

Response body:

```json
{
  "serviceRequest": {
    "id": "sr-uuid",
    "requestCode": "SR-2026-0001",
    "vesselId": 1,
    "vesselName": "Serenity IV",
    "ownerName": "Ahmet Yılmaz",
    "providerName": "Bodrum Marine Service",
    "serviceType": "Annual Survey",
    "categoryName": "Survey",
    "status": "completed",
    "priority": "normal",
    "location": "Bodrum Marina",
    "description": "...",
    "notes": "...",
    "createdAt": "2026-06-19T12:00:00Z",
    "requestedDate": "2026-06-21T09:00:00Z",
    "timeline": [],
    "offers": [],
    "assignments": [],
    "workLogs": [],
    "attachments": [],
    "completion": null,
    "dispute": null
  },
  "warnings": []
}
```

### 3. GET `/api/v1/admin-panel/service-requests/by-vessel/{vesselId}/history`

Dedicated vessel history endpoint for Vessel Detail or debug/smoke tests.

Query parameters:
- `take` default 10
- `statuses[]` optional, default includes completed/resolved/in-progress according to UI need

Response body:

```json
{
  "serviceHistory": [
    {
      "id": "sr-uuid",
      "date": "2026-03-10T00:00:00Z",
      "serviceType": "Annual Survey",
      "provider": "Turkish Lloyd Maritime",
      "location": "Bodrum Marina",
      "notes": "All certificates renewed",
      "status": "completed"
    }
  ],
  "warnings": []
}
```

## P1 supporting read endpoints

- GET `/api/v1/admin-panel/service-requests/{id}/timeline`
- GET `/api/v1/admin-panel/service-requests/{id}/offers`
- GET `/api/v1/admin-panel/service-requests/{id}/assignments`
- GET `/api/v1/admin-panel/service-requests/{id}/worklogs`
- GET `/api/v1/admin-panel/service-requests/{id}/messages`
- GET `/api/v1/admin-panel/service-requests/{id}/completion-dispute`

## P2 command endpoints

Expose only if corresponding ServiceRequest commands already exist:

- POST `/api/v1/admin-panel/service-requests/{id}/assign`
- POST `/api/v1/admin-panel/service-requests/{id}/status`
- POST `/api/v1/admin-panel/service-requests/{id}/complete/approve`
- POST `/api/v1/admin-panel/service-requests/{id}/dispute/resolve`

If the command does not exist, return 501 or document as a gap. Do not invent domain commands in BFF.
