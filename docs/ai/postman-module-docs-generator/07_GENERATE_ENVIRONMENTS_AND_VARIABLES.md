# 07 - Generate Environments and Variables

Create:

```text
docs/postman/InktaviaMarineOS.Local.postman_environment.json
docs/postman/InktaviaMarineOS.Dev.postman_environment.json
```

Include existing variables and add missing active-module variables.

Required local root URLs:

- identity_api_root_url = http://localhost:7101
- reference_data_api_root_url = http://localhost:7104
- vessel_api_root_url = http://localhost:7105
- file_storage_api_root_url = http://localhost:7106
- service_request_api_root_url = http://localhost:7107

Required base URLs:

- identity_api_base_url = {{identity_api_root_url}}/api/v1
- reference_data_api_base_url = {{reference_data_api_root_url}}/api/v1
- vessel_api_base_url = {{vessel_api_root_url}}/api/v1
- file_storage_api_base_url = {{file_storage_api_root_url}}/api/v1
- service_request_api_base_url = {{service_request_api_root_url}}/api/v1

Required token variables:

- active_access_token
- mobile_access_token
- customer_access_token
- admin_access_token
- identityAccessToken
- X-Aizen-User-Token
- identityRefreshToken

Required scenario ids:

- vesselId
- fileId
- serviceRequestId
- serviceRequestOfferId
- assignmentId
- messageId
- workLogId
- completionId
- disputeId
- currencyId
- lookupGroupId
- lookupItemId
