# Admin Panel BFF — Postman Testing Guide

## Prerequisites

- Docker Compose stack running (`docker-compose up`)
- Admin Panel BFF running on `http://localhost:5200`
- Keycloak running (default: `http://localhost:8080`)
- Postman Desktop ≥ v10 or Postman CLI (`newman`)

---

## Running BFF Locally

```bash
# From repository root
cd Bff/src/AdminPanel/Aizen.Bff.AdminPanel
dotnet run
```

The BFF will start on `https://localhost:5200` (or the port configured in `launchSettings.json`).

Swagger UI: `http://localhost:5200/swagger`

---

## Environment Variables

Set these in your `appsettings.Development.json` or via environment:

```json
{
  "RemoteCall": {
    "IIdentityAdminBffRemoteCall": { "BaseAddress": "http://localhost:7101" },
    "IReferenceDataAdminBffRemoteCall": { "BaseAddress": "http://localhost:7104" },
    "IVesselAdminBffRemoteCall": { "BaseAddress": "http://localhost:7105" },
    "IFileStorageAdminBffRemoteCall": { "BaseAddress": "http://localhost:7106" },
    "IServiceRequestAdminBffRemoteCall": { "BaseAddress": "http://localhost:7107" }
  },
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/inktavia",
    "Audience": "inktavia-api"
  }
}
```

---

## Getting an Admin JWT Token from Keycloak

### Using curl

```bash
curl -s -X POST \
  "http://localhost:8080/realms/inktavia/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=inktavia-api" \
  -d "client_secret=YOUR_CLIENT_SECRET" \
  -d "username=admin@inktavia.com" \
  -d "password=YOUR_ADMIN_PASSWORD" \
  | jq -r '.access_token'
```

Copy the output and use it as `{{admin_token}}` in Postman.

### Using Postman Pre-Request Script

In the Postman Environment, the collection's `Auth Setup` request automatically fetches a token and sets `{{admin_token}}`.

---

## Setting Up Postman Environment

1. Open Postman → **Environments** → **New**
2. Name it: `Admin Panel BFF - Local`
3. Add variables:

| Variable | Initial Value | Current Value |
|---|---|---|
| `base_url` | `http://localhost:5200` | `http://localhost:5200` |
| `keycloak_url` | `http://localhost:8080` | `http://localhost:8080` |
| `keycloak_realm` | `inktavia` | `inktavia` |
| `keycloak_client_id` | `inktavia-api` | `inktavia-api` |
| `keycloak_client_secret` | *(your secret)* | *(your secret)* |
| `admin_username` | `admin@inktavia.com` | `admin@inktavia.com` |
| `admin_password` | *(your password)* | *(your password)* |
| `admin_token` | *(leave empty — auto-filled)* | |
| `sample_profile_id` | *(a real profile Guid)* | |
| `sample_vessel_id` | `1` | |
| `sample_service_request_id` | `1` | |
| `sample_dispute_id` | `1` | |
| `sample_file_id` | *(a real file Guid)* | |
| `sample_user_id` | `1` | |

---

## Importing Collections

1. In Postman → **Import**
2. Select:
   - `AdminPanelBff.ControllerApiTests.postman_collection.json`
   - `AdminPanelBff.BusinessScenarios.postman_collection.json`
3. Select the `Admin Panel BFF - Local` environment

---

## Running the Controller API Tests Collection

- Runs every endpoint once to verify HTTP 200 / expected structure
- Run individual folders (Dashboard, Identity, Vessels, etc.) or the full collection
- Use **Collection Runner** for sequential execution with results summary

```bash
# Using newman CLI
newman run AdminPanelBff.ControllerApiTests.postman_collection.json \
  -e AdminPanelBff-Local.postman_environment.json \
  --reporters cli,json \
  --reporter-json-export results.json
```

---

## Running the Business Scenarios Collection

- Each folder represents one end-to-end scenario
- Scenarios chain requests using `pm.environment.set()` to pass IDs between steps
- Run in order within each folder — do not run individual requests in isolation

| Scenario | Description |
|---|---|
| 1. Dashboard Load | Verify admin can load overview |
| 2. Approve Organizer | Search → detail → approve flow |
| 3. Archive & Restore Vessel | Archive → verify archived → restore |
| 4. Resolve Dispute | List disputes → get detail → resolve |
| 5. Bulk File Read URLs | Generate pre-signed URLs for multiple files |

---

## Common Issues

| Problem | Fix |
|---|---|
| `401 Unauthorized` | Token expired or missing — re-run `Auth Setup` request |
| `403 Forbidden` | User does not have `Admin` role in Keycloak |
| `Connection refused` | BFF or target module is not running |
| `404 Not Found` on module | Module service is down or wrong port configured |
| Empty response body | Check module logs; remote call may have returned an error |
