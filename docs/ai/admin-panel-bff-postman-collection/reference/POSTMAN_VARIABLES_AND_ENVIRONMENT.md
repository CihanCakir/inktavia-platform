# Postman Variables and Environment

Create:

```text
docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF_Local.postman_environment.json
```

Required variables:

```text
admin_panel_bff_root_url = http://localhost:<ADMIN_BFF_PORT>
admin_panel_bff_base_url = {{admin_panel_bff_root_url}}/api/v1/admin-panel
identityAccessToken = 
identityRefreshToken = 
X_Aizen_User_Token = Bearer <identityAccessToken>
sampleProfileId = 00000000-0000-0000-0000-000000000000
sampleOrganizerProfileId = 00000000-0000-0000-0000-000000000000
sampleVenueProfileId = 00000000-0000-0000-0000-000000000000
sampleParticipantProfileId = 00000000-0000-0000-0000-000000000000
sampleUserId = 1
sampleFileId = 1
sampleVesselId = 1
sampleDocumentId = 1
sampleServiceRequestId = 1
sampleDisputeId = 1
sampleCountryId = 1
sampleLookupGroupCode = vessel_type
samplePageIndex = 0
samplePageSize = 20
keycloak_token_url = http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token
admin_panel_bff_client_id = admin-panel-bff
admin_panel_bff_client_secret = local-dev-only-change-me
adminPanelBffServiceToken = 
```

Do not commit real production secrets.
