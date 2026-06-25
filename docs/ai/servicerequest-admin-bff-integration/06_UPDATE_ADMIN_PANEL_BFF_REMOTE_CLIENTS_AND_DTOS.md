# 06 — Update AdminPanel BFF Remote Clients and DTOs

Update or create `IServiceRequestAdminBffRemoteCall` in AdminPanel BFF Application.

## RemoteCall methods

Use Refit-style signatures matching existing BFF RemoteCall pattern. Include:

- `GetAdminServiceRequestList(...)`
- `GetAdminServiceRequestDetail(id)`
- `GetServiceHistoryByVessel(vesselId, take)`
- `GetServiceRequestTimeline(id)`
- `GetServiceRequestOffers(id)`
- `GetServiceRequestAssignments(id)`
- `GetServiceRequestWorkLogs(id)`
- `GetServiceRequestCompletionDispute(id)`

Only include methods for endpoints implemented or already existing in ServiceRequest module. Document missing ones.

## BFF DTOs

Create BFF-owned DTOs under the existing AdminPanel BFF pattern. Suggested conceptual DTOs:

- `ServiceRequestListItemBffDto`
- `ServiceRequestPageBffDto`
- `AdminServiceRequestListBffResponse`
- `ServiceRequestDetailBffDto`
- `AdminServiceRequestDetailBffResponse`
- `ServiceRequestTimelineItemBffDto`
- `ServiceRequestOfferBffDto`
- `ServiceRequestAssignmentBffDto`
- `ServiceRequestWorkLogBffDto`
- `ServiceRequestCompletionDisputeBffDto`
- `ServiceHistoryItemBffDto`

## Serialization rule

Do not expose `IPaginate<T>` or interface properties in any RemoteCall response shape.
