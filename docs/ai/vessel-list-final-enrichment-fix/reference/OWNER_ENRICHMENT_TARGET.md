# Owner Enrichment Target

## Problem

The live response includes owner IDs but not display data:

```json
{
  "ownerUserId": 10003,
  "ownerProfileId": 11003
}
```

## Target

The AdminPanel BFF must enrich the current page with owner display fields:

```json
{
  "ownerUserId": 10003,
  "ownerProfileId": 11003,
  "ownerName": "Ayşe Demir",
  "ownerAvatarUrl": null
}
```

## Rules

- Collect distinct `OwnerUserId` values from the page.
- Make exactly one Identity bulk call per page.
- Do not call Identity once per vessel row.
- Key enrichment dictionary by `UserId`, not by `ProfileId`.
- Use FullName if present; otherwise FirstName + LastName; otherwise DisplayName.
- Use ProfilePhotoUrl if present; otherwise AvatarUrl.
- If Identity fails, add `AdminBffWarning.ModuleUnavailable("Identity")` and keep rows.

## Expected Identity Endpoint

```http
GET /api/v1/identity/admin/users/profiles/bulk?userIds=10003&userIds=10004
```

If the actual Identity route differs, align the BFF Refit/RemoteCall interface with the actual route.
