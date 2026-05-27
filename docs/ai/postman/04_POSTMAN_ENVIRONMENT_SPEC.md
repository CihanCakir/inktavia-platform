# Postman Environment Specification

## Required File

Create:

```text
infrastructure/postman/inktavia-local.postman_environment.json
```

The file must be importable by Postman.

## Environment Name

```text
Inktavia Local
```

## Required Variables

### Keycloak

```text
keycloak_base_url=http://localhost:8080
keycloak_realm=inktavia-realm
keycloak_token_url={{keycloak_base_url}}/realms/{{keycloak_realm}}/protocol/openid-connect/token
```

### API Base URLs

The agent must infer actual API ports from docker-compose.yaml if available.

Default fallback values:

```text
identity_api_base_url=http://localhost:5001/api/v1/identity
profile_api_base_url=http://localhost:5002/api/v1
payment_api_base_url=http://localhost:5003/api/v1
```

If actual service ports differ, update these values from docker-compose.yaml.

### Application Clients

```text
mobile_client_id=inktavia-mobile
customer_panel_client_id=customer-panel
admin_panel_client_id=admin-panel
```

### API Audiences

```text
identity_api_audience=identity-api
profile_api_audience=profile-api
payment_api_audience=payment-api
```

### Test Users

```text
mobile_username=mobile.user@inktavia.com
customer_username=customer.user@inktavia.com
admin_username=admin.user@inktavia.com
default_password=Password123!
```

### Token Variables

```text
mobile_access_token=
customer_access_token=
admin_access_token=
active_access_token=
```

### Sync Metadata

```text
postman_generated_folder=04 - Auto-Discovered Endpoints
controller_scan_root=src/MDYKE
```

## Secret Handling

Local test password can exist in local-only Postman environment.

Do not commit real production passwords, client secrets, or tokens.

If a client secret is required later, create empty variables:

```text
customer_panel_client_secret=
admin_panel_client_secret=
```

Do not hardcode secret values.
