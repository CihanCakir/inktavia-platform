# Active Module Debug Playbook

For every failed active endpoint:

1. Locate the AdminPanel BFF controller/action.
2. Locate the BFF Application command/query/handler.
3. Locate the RemoteCall interface.
4. Locate the internal module controller action.
5. Verify route, method, request body, response type.
6. Verify outgoing headers:
   - `Authorization: Bearer <admin-panel-bff-keycloak-service-token>`
   - `X-Aizen-User-Token: Bearer <identityAccessToken>`
7. Verify Keycloak service-token has correct audience and client roles.
8. Verify Identity token is forwarded unchanged.
9. Debug actual module error, not just BFF status.
10. Normalize response shape only after the remote call succeeds or fails with known domain errors.

Do not hide module errors by returning dummy success.
