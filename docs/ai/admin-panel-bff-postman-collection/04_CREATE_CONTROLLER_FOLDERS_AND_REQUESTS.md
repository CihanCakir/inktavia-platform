# 04 — Create Controller Folders and Requests

Create requests for all catalog endpoints under folders:

- 01 - Dashboard
- 02 - Identity - General Profiles
- 03 - Identity - Organizer Profiles
- 04 - Identity - Venue Profiles
- 05 - Identity - Participant Profiles
- 06 - Files
- 07 - Vessels
- 08 - Service Requests
- 09 - Reference Data

Use `{{admin_panel_bff_base_url}}` for every URL.

Protected requests must send only:

```http
X-Aizen-User-Token: {{X_Aizen_User_Token}}
```

Do not attach Authorization to these requests.
