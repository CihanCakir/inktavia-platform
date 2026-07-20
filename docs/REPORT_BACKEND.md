# Notification Module — Architecture-Compliant Refactor — Backend Report

## Summary
Notification module refactored to align with platform architecture conventions. All band-aid fixes (FIX-2/3/4)
and `/provider` sub-route workarounds are superseded by this permanent, correct implementation.

## Changes

### Controllers → `AizenWebApiController` + typed envelope
- **`NotificationsController`**: Now extends `AizenWebApiController`, uses `IAizenCQRSProcessor` instead of `ISender`.
  All endpoints return typed `AizenApiResponse<T>` with `[ProducesResponseType]`. Identity is no longer read in
  the controller — handlers resolve it internally. **`/provider` sub-routes removed** (GET provider, mark-provider-read,
  mark-all-provider-read) — the standard routes now serve both user and provider via the unified `effectiveRecipientId`.
- **`NotificationTemplatesController`**: Same refactor — extends `AizenWebApiController`, all CRUD routed through CQRS.
  Repository is no longer injected directly; all logic moved to handlers.

### Handler-internal identity resolution (`effectiveRecipientId`)
All recipient-based handlers (`GetUserNotificationsQueryHandler`, `MarkNotificationAsReadCommandHandler`,
`BulkMarkAsReadCommandHandler`, `RegisterDeviceTokenCommandHandler`, `RegisterWebPushSubscriptionCommandHandler`)
now inject `IAizenInfoAccessor` and resolve the effective recipient:
```
effectiveRecipientId =
    _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId is > 0 and var pid
        ? pid
        : _info.UserInfoAccessor.UserInfo.UserId;
```
This is consistent with how consumers write `RecipientUserId = ProfileId` for provider notifications.

### Command/Query type changes
| Type | Removed fields | New Response type |
|------|---------------|-------------------|
| `GetUserNotificationsQuery` | `UserId` | `NotificationListResponse` (Abstraction) |
| `MarkNotificationAsReadCommand` | `RequestingUserId` | `MarkNotificationReadResponse` |
| `BulkMarkAsReadCommand` | `UserId` | `MarkAllNotificationsReadResponse` |
| `RegisterDeviceTokenCommand` | `UserId` | `DeviceTokenResponse` |
| `RegisterWebPushSubscriptionCommand` | `UserId` | `PushSubscriptionResponse` |
| `DeactivateWebPushSubscriptionCommand` | (none) | `PushSubscriptionResponse` |
| `SendNotificationCommand` | (none — `RecipientUserId` kept) | `SendNotificationResponse` |

### Template CRUD → CQRS
New commands/queries created:
- `GetNotificationTemplateByCodeQuery` → `NotificationTemplateDto?`
- `CreateNotificationTemplateCommand` → `NotificationTemplateMutationResponse`
- `UpdateNotificationTemplateCommand` → `NotificationTemplateMutationResponse`
- `ToggleNotificationTemplateCommand` → `NotificationTemplateMutationResponse`

### Request/Response types → Abstraction
- **`Abstraction/Response/`**: `NotificationListResponse`, `MarkNotificationReadResponse`,
  `MarkAllNotificationsReadResponse`, `PushSubscriptionResponse`, `DeviceTokenResponse`,
  `SendNotificationResponse`, `NotificationTemplateListResponse`, `NotificationTemplateMutationResponse`
- **`Abstraction/Request/`**: `CreateNotificationTemplateRequest`, `UpdateNotificationTemplateRequest`,
  `RegisterDeviceTokenRequest` (moved from controller-inline)
- `GetUserNotificationsResponse` (was in Application) → replaced by `NotificationListResponse` in Abstraction
- `SendNotificationCommandResponse` (was in Application) → replaced by `SendNotificationResponse` in Abstraction

### Repository change
- `INotificationRepository.BulkMarkAsReadAsync` now returns `Task<int>` (affected row count) instead of `Task`.

### Application csproj
- Added `Aizen.Core.InfoAccessor.Abstraction` project reference (for `IAizenInfoAccessor` in handlers).

### BFF — Marine Provider
- **`INotificationRemoteCall`**: Routes changed from `/provider` sub-routes to standard
  `/api/v1/notification/notifications`. Return types changed from `AizenApiResponse<object>` /
  `ProviderNotificationsResponse` to typed Abstraction responses (`NotificationListResponse`,
  `MarkNotificationReadResponse`, `MarkAllNotificationsReadResponse`, `PushSubscriptionResponse`).
- **`ProviderNotificationsResponse`** (BFF-local mirror DTO) → **deleted**, replaced by
  `NotificationListResponse` from Abstraction.
- **BFF commands/queries**: `MarkNotificationReadBffCommand`, `MarkAllNotificationsReadBffCommand`,
  `SubscribePushCommand`, `UnsubscribePushCommand` → typed responses from Abstraction instead of `object`.
  `SubscribePushResponse` / `UnsubscribePushResponse` (BFF-local) → **deleted**, replaced by
  `PushSubscriptionResponse` from Abstraction.
- **BFF `NotificationsController`**: All endpoints return typed `AizenApiResponse<T>` with `[ProducesResponseType]`.
  FE-facing routes (`/api/v1/provider/notifications/...`) **unchanged**.

### BFF — Admin Panel (consumer compatibility)
- **`INotificationBffRemoteCall`**: Switched from plain Refit `[Get]`/`[Patch]`/`[Post]` to
  `AizenRemoteCall` attributes, now expects `AizenApiResponse<T>` envelope. Controller updated to unwrap `.Body`.
- **`INotificationAdminBffRemoteCall`**: Create/Update/Toggle return types changed from
  `AizenApiResponse<object>` to `AizenApiResponse<NotificationTemplateMutationResponse>`.
  Handlers don't inspect `.Body` — no handler changes needed.

## Consumer compatibility
All consumers of notification routes were identified and updated:
- **Marine Provider BFF**: Updated (see above)
- **Admin Panel BFF**: Updated — both `INotificationBffRemoteCall` and `INotificationAdminBffRemoteCall`
  now expect enveloped responses. No silent breakage.
- **Cross-module consumers** (CargoDry, ServiceRequest, Payment, Identity, Messaging via MassTransit):
  Use `SendNotificationCommand` which is unchanged (`RecipientUserId` preserved). Not affected.

## Build verification
```
Notification module:     0 Error(s)
Marine Provider BFF:     0 Error(s)
Admin Panel BFF:         0 Error(s)
```
