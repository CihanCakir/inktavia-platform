# Keycloak Realm Requirements

Use one realm:

```text
inktavia-realm
```

## Confidential BFF client

Create or maintain:

```text
admin-panel-bff
```

Configuration:

```text
Client authentication: ON
Service accounts: ON
Standard flow: OFF
Direct access grants: OFF
Implicit flow: OFF
```

## API/resource clients

Create or maintain:

```text
identity-api
reference-data-api
vessel-api
file-storage-api
service-request-api
notification-api
```

## Role examples

```text
identity-api:
  identity.auth
  identity.read
  identity.write
  identity.admin
  identity.profile.read
  identity.profile.approve
  identity.profile.reject

reference-data-api:
  reference-data.read
  reference-data.write
  reference-data.lookup.manage
  reference-data.location.read
  reference-data.currency.manage

vessel-api:
  vessel.read
  vessel.write
  vessel.admin
  vessel.document.manage
  vessel.ownership.manage

file-storage-api:
  file.read
  file.write
  file.delete
  file.read-url.create
  file.upload-url.create
  file.visibility.manage

service-request-api:
  service-request.read
  service-request.write
  service-request.admin
  service-request.assignment.manage
  service-request.dispute.manage
  service-request.completion.manage
```

## Audience

If internal APIs validate `aud`, configure audience mappers so `admin-panel-bff` service tokens contain:

```text
identity-api
reference-data-api
vessel-api
file-storage-api
service-request-api
```

## Service account roles

Assign the required API client roles to the `admin-panel-bff` service account.

Do not give broad realm-admin permissions unless explicitly required for a real admin operation.
