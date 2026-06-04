# 08 - Controllers, Routes and Naming Cleanup

Review AdminPanel BFF controllers and clean naming/routing.

## Rules

- The BFF is already AdminPanel. Do not prefix every controller with `Admin`.
- Controllers must not call AizenRemoteCall directly.
- Controllers must use Application commands/queries.
- Keep route prefix AdminPanel-specific where required.

Recommended route base:

```text
/api/v1/admin-panel
```

Examples:

```text
/api/v1/admin-panel/auth/login/username
/api/v1/admin-panel/users/{userId}/overview
/api/v1/admin-panel/profiles/filter
/api/v1/admin-panel/reference-data/lookup-groups
/api/v1/admin-panel/vessels/{vesselId}/overview
/api/v1/admin-panel/service-requests/{id}/operation-detail
```

Generate a controller route table:

```text
Bff/src/AdminPanel/docs/admin-panel-bff-controller-route-map.md
```
