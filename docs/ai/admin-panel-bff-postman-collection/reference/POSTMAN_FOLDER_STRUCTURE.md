# Postman Folder Structure

Create folders in this exact order:

```text
00 - Auth Setup
00 - Keycloak BFF Service Token Validation (Optional)
01 - Dashboard
02 - Identity - General Profiles
03 - Identity - Organizer Profiles
04 - Identity - Venue Profiles
05 - Identity - Participant Profiles
06 - Files
07 - Vessels
08 - Service Requests
09 - Reference Data
```

Folder rules:

- Auth Setup contains Identity login, OTP, refresh, password change.
- Keycloak BFF Service Token Validation is optional and isolated.
- Controller folders contain only AdminPanel BFF routes.
- Do not create direct internal API route folders.
- Do not create Payment/Profile active folders unless real BFF endpoints exist and they are active.
