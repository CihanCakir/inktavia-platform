# 04 - Complete Identity Authentication, Authorization and Endpoints

Complete all missing Identity-related AdminPanel BFF endpoints.

## Must include authentication/authorization endpoints where applicable

Audit and add BFF support for discovered Identity endpoints including but not limited to:

- LoginWithUsername
- LoginWithPhoneNumber
- LoginWithOtp
- SendOtp
- CheckOtp
- Refresh / RefreshLogin
- ChangePassword
- Current user/profile detail
- User profile detail
- User profile list
- User profile with roles
- User profiles by filter
- Organizer profile approve/reject/update/detail/list/filter
- Participant profile update/detail/list/filter
- Venue profile approve/reject/update/detail/list/filter
- Role/profile operations if discovered

## Structure

Use controllers such as:

```text
AuthenticationController
UsersController
ProfilesController
OrganizersController
ParticipantsController
VenuesController
RolesController
```

Do not prefix every controller with `Admin`.

## Application structure

Use separate command/query folders and files.

Examples:

```text
Application/Authentication/Command/LoginWithUsername/LoginWithUsernameCommand.cs
Application/Authentication/Command/LoginWithUsername/LoginWithUsernameCommandHandler.cs
Application/Users/Query/GetUserProfileDetail/GetUserProfileDetailQuery.cs
Application/Users/Query/GetUserProfileDetail/GetUserProfileDetailQueryHandler.cs
```

## Remote call

Use IdentityRemoteService via AizenRemoteCall.

Forward both tokens:

```text
Authorization: Bearer {Keycloak token}
X-Aizen-User-Token: {Identity token}
```

Generate/update Identity endpoint coverage in:

```text
Bff/src/AdminPanel/docs/postman/admin-panel-bff-endpoint-inventory.md
```
