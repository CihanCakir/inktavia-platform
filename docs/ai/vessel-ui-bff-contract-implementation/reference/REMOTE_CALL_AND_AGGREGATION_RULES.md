# RemoteCall and Aggregation Rules

## BFF remote clients

Extend existing Refit/AizenRemoteCall interfaces. Do not create duplicate HTTP clients if a remote client already exists.

## Service token model

BFF to modules must use:

- Keycloak service token in `Authorization`
- Identity user token in `X-Aizen-User-Token`

Do not forward browser Keycloak assumptions into this flow.

## Null-safe cross-module aggregation

CargoDry and ServiceRequest calls are optional for MVP.

If the remote client or module is not available:

- Return `cargoDryKits: []`
- Return `serviceHistory: []`
- Add a report entry; do not fail the vessel detail page.

## Parallel calls

Use `Task.WhenAll` only for independent calls and only after safe task creation. Catch and isolate optional dependency failures.

## Serialization rule

Avoid `IPaginate<T>` in DTOs returned over HTTP/Refit. Use:

- concrete `Paginate<T>` when aligned with module contracts, or
- BFF-owned `PageBffDto<T>`.

This prevents `System.Text.Json` interface deserialization failures.
