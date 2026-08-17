# PROMPT 3 — Notification Backend Implementation + Frontend Inbox

## Scope

This prompt covers two distinct layers:

**Part A — Backend implementation** of `Aizen.Modules.Notification` using the 3 existing prompt files from `notification-module-v1/` as the authoritative specification. This prompt adds CargoDry-specific consumer wiring and clarifies execution order.

**Part B — Frontend** notification infrastructure:
- `notification.types.ts` extension (in-app notification shape)
- `useNotificationsQuery.ts` — polling hook for unread count + inbox list
- `useNotificationHub.ts` — SignalR hook for real-time push
- `useMarkNotificationRead.ts` — mutation
- `NotificationBell.tsx` — header badge component
- `NotificationsInboxPage.tsx` — full inbox page

---

## Reference Documents

Before starting, read these prompt files in order:

1. `notification-module-v1/PROMPT_A_NOTIFICATION_FOUNDATION.md` — Domain, Entities, Repositories
2. `notification-module-v1/PROMPT_B_NOTIFICATION_APPLICATION.md` — CQRS, Consumers, Dispatchers
3. `notification-module-v1/PROMPT_C_NOTIFICATION_API.md` — SignalR Hub, Controllers, BFF, Program.cs

Those 3 prompts are the complete backend specification. This prompt **adds** the following on top:

---

## PART A — CargoDry-Specific Consumer Wiring

The Notification module's PROMPT_B already defines consumer placeholders for CargoDry events. This section provides the concrete handler logic for each.

### A.1 CargoDryKitActivatedConsumer

**Consumes:** `CargoDryKitActivatedMessage`

**Target users:** `OwnerUserId` on the kit

**Template key:** `cargodry.kit.activated`

**Variables:**
```
{{kitCode}}        — e.g. "CD-2025-0001234"
{{serialNumber}}   — e.g. "ABCD-EFGH-IJKL-MNOP"
{{productName}}    — e.g. "CargoDry Standard 90"
{{expiryDate}}     — formatted "15 Jan 2026"
{{vesselName}}     — e.g. "S/Y Inktavia"
```

**Logic in `ExecuteCommitMessage`:**

```csharp
protected override async Task<bool> ExecuteCommitMessage(
    CargoDryKitActivatedMessage message,
    CancellationToken cancellationToken)
{
    if (message.OwnerUserId is null) return true; // unassigned kit, skip

    await _mediator.Send(new SendNotificationCommand
    {
        UserId       = message.OwnerUserId.Value,
        Type         = NotificationType.CargoDryKitActivated,   // = 300
        Channel      = NotificationChannel.InApp,
        TemplateKey  = "cargodry.kit.activated",
        Variables    = new Dictionary<string, string>
        {
            ["kitCode"]      = message.KitCode,
            ["serialNumber"] = message.SerialNumber,
            ["productName"]  = message.ProductName,
            ["expiryDate"]   = message.ExpiresAt.ToString("dd MMM yyyy"),
            ["vesselName"]   = message.VesselName ?? "—",
        },
        RelatedEntityType = "CargoDryKit",
        RelatedEntityId   = message.KitId.ToString(),
    }, cancellationToken);

    return true;
}
```

---

### A.2 CargoDryKitExpiringConsumer

**Consumes:** `CargoDryKitExpiringMessage`  
Published by: `KitExpiryReminderJob` at 30d / 7d / 1d before expiry

**Template key:** `cargodry.kit.expiring`

**Variables:**
```
{{kitCode}}
{{daysUntilExpiry}}   — "30", "7", or "1"
{{expiryDate}}        — formatted date
{{productName}}
{{vesselName}}
```

**Channels:** `InApp` + `Push` (dual dispatch via CompositeDispatcher)

```csharp
await _mediator.Send(new SendNotificationCommand
{
    UserId       = message.OwnerUserId,
    Type         = NotificationType.CargoDryKitExpiring,   // = 301
    Channel      = NotificationChannel.Push,               // composite dispatcher sends both InApp + Push
    TemplateKey  = "cargodry.kit.expiring",
    Variables    = new Dictionary<string, string>
    {
        ["kitCode"]          = message.KitCode,
        ["daysUntilExpiry"]  = message.DaysUntilExpiry.ToString(),
        ["expiryDate"]       = message.ExpiresAt.ToString("dd MMM yyyy"),
        ["productName"]      = message.ProductName,
        ["vesselName"]       = message.VesselName ?? "—",
    },
    RelatedEntityType = "CargoDryKit",
    RelatedEntityId   = message.KitId.ToString(),
}, cancellationToken);
```

---

### A.3 CargoDryKitExpiredConsumer

**Consumes:** `CargoDryKitExpiredMessage`  
Published by: `KitExpiredMarkingJob` (hourly)

**Template key:** `cargodry.kit.expired`

**Variables:** same shape as expiring, minus `daysUntilExpiry`

**Channel:** `InApp` only

---

### A.4 CargoDryKitRenewedConsumer

**Consumes:** `CargoDryKitRenewedMessage`

**Template key:** `cargodry.kit.renewed`

**Variables:**
```
{{kitCode}}
{{productName}}
{{newExpiryDate}}     — formatted new ExpiresAt
{{renewalCount}}      — how many times renewed
{{renewalType}}       — "OnlinePurchase" | "PhysicalKit" | "AdminExtension"
```

**Channel:** `InApp`

---

### A.5 CargoDryKitRevokedConsumer

**Consumes:** `CargoDryKitRevokedMessage`

**Template key:** `cargodry.kit.revoked`

**Variables:**
```
{{kitCode}}
{{productName}}
{{revokedAt}}         — formatted date
{{reason}}
```

**Channel:** `InApp`  
**Note:** Only notify if `OwnerUserId` is non-null (kit may be unassigned).

---

### A.6 Notification Templates Seed

Add these 5 templates to the `NotificationTemplate` seed data:

```json
[
  {
    "key": "cargodry.kit.activated",
    "name": "CargoDry Kit Activated",
    "category": "Transactional",
    "channels": ["InApp"],
    "subject": "Your CargoDry kit is now active",
    "bodyInApp": "Kit {{kitCode}} ({{productName}}) activated on {{vesselName}}. Valid until {{expiryDate}}.",
    "variables": ["kitCode", "serialNumber", "productName", "expiryDate", "vesselName"],
    "status": "Active"
  },
  {
    "key": "cargodry.kit.expiring",
    "name": "CargoDry Kit Expiring",
    "category": "Alert",
    "channels": ["InApp", "Push"],
    "subject": "CargoDry kit expires in {{daysUntilExpiry}} days",
    "bodyInApp": "Kit {{kitCode}} ({{productName}}) on {{vesselName}} will expire on {{expiryDate}}. Renew to maintain protection.",
    "bodyPush": "Kit {{kitCode}} expires in {{daysUntilExpiry}} days.",
    "variables": ["kitCode", "daysUntilExpiry", "expiryDate", "productName", "vesselName"],
    "status": "Active"
  },
  {
    "key": "cargodry.kit.expired",
    "name": "CargoDry Kit Expired",
    "category": "Alert",
    "channels": ["InApp"],
    "subject": "CargoDry kit expired",
    "bodyInApp": "Kit {{kitCode}} ({{productName}}) on {{vesselName}} has expired.",
    "variables": ["kitCode", "productName", "vesselName"],
    "status": "Active"
  },
  {
    "key": "cargodry.kit.renewed",
    "name": "CargoDry Kit Renewed",
    "category": "Transactional",
    "channels": ["InApp"],
    "subject": "CargoDry kit renewed",
    "bodyInApp": "Kit {{kitCode}} renewed ({{renewalType}}). New expiry: {{newExpiryDate}}. Total renewals: {{renewalCount}}.",
    "variables": ["kitCode", "productName", "newExpiryDate", "renewalCount", "renewalType"],
    "status": "Active"
  },
  {
    "key": "cargodry.kit.revoked",
    "name": "CargoDry Kit Revoked",
    "category": "System",
    "channels": ["InApp"],
    "subject": "CargoDry kit revoked",
    "bodyInApp": "Kit {{kitCode}} ({{productName}}) has been revoked. Reason: {{reason}}.",
    "variables": ["kitCode", "productName", "revokedAt", "reason"],
    "status": "Active"
  }
]
```

---

## PART B — Frontend: Notification Inbox

### B.1 Extend notification.types.ts

**File:** `src/shared/api/types/notification.types.ts`

Add the following (keep existing template-related types, append below):

```typescript
// ── In-App Notification (user-facing inbox) ───────────────────────────────────

export type InAppNotificationStatus = 'Unread' | 'Read' | 'Archived'

export interface InAppNotificationDto {
  id:                number
  type:              string         // NotificationType value as string e.g. "CargoDryKitExpiring"
  title:             string         // rendered subject from template
  body:              string         // rendered body from template
  isRead:            boolean
  status:            InAppNotificationStatus
  relatedEntityType: string | null  // "CargoDryKit", "ServiceRequest", etc.
  relatedEntityId:   string | null
  createdAt:         string         // ISO
  readAt:            string | null
}

export interface InAppNotificationListDto {
  items:       InAppNotificationDto[]
  total:       number
  unreadCount: number
  page:        number
  pageSize:    number
}

export interface NotificationUnreadCountDto {
  unreadCount: number
}

export interface MarkNotificationReadRequest {
  notificationId: number
}

export interface MarkAllNotificationsReadRequest {
  userId?: number   // admin-only; omit for current user
}
```

---

### B.2 Add Endpoints

**File:** `src/shared/api/endpoints.ts` — add section:

```typescript
// Notifications (in-app inbox — user-facing via Notification module BFF)
NOTIFICATIONS:                   '/notifications',
NOTIFICATIONS_UNREAD_COUNT:      '/notifications/unread-count',
NOTIFICATIONS_MARK_READ:         (id: number) => `/notifications/${id}/read`,
NOTIFICATIONS_MARK_ALL_READ:     '/notifications/mark-all-read',
```

---

### B.3 Add Query Keys

**File:** `src/shared/api/queryKeys.ts` — add:

```typescript
notifications: {
  all:         ['notifications'] as const,
  list:        (page?: number) => ['notifications', 'list', page] as const,
  unreadCount: ()              => ['notifications', 'unread-count'] as const,
},
```

---

### B.4 useNotificationsQuery

**New file:** `src/features/notifications/hooks/useNotificationsQuery.ts`

```typescript
import { useQuery }     from '@tanstack/react-query'
import httpClient       from '@shared/api/httpClient'
import { ENDPOINTS }    from '@shared/api/endpoints'
import { queryKeys }    from '@shared/api/queryKeys'
import { normalizeSuccess, normalizeFailure } from '@shared/api/responseNormalizer'
import type { InAppNotificationListDto } from '@shared/api/types/notification.types'

export function useNotificationsQuery(page = 1) {
  return useQuery({
    queryKey: queryKeys.notifications.list(page),
    queryFn: async () => {
      try {
        const r = await httpClient.get(ENDPOINTS.NOTIFICATIONS, {
          params: { page, pageSize: 20 },
        })
        const result = normalizeSuccess<InAppNotificationListDto>(r)
        return result.ok ? result.data : null
      } catch (e) {
        normalizeFailure(e)
        return null
      }
    },
    staleTime: 30_000,
  })
}
```

---

### B.5 useNotificationUnreadCount

**New file:** `src/features/notifications/hooks/useNotificationUnreadCount.ts`

```typescript
import { useQuery }     from '@tanstack/react-query'
import httpClient       from '@shared/api/httpClient'
import { ENDPOINTS }    from '@shared/api/endpoints'
import { queryKeys }    from '@shared/api/queryKeys'
import { normalizeSuccess, normalizeFailure } from '@shared/api/responseNormalizer'
import type { NotificationUnreadCountDto } from '@shared/api/types/notification.types'

export function useNotificationUnreadCount() {
  return useQuery({
    queryKey: queryKeys.notifications.unreadCount(),
    queryFn: async () => {
      try {
        const r = await httpClient.get(ENDPOINTS.NOTIFICATIONS_UNREAD_COUNT)
        const result = normalizeSuccess<NotificationUnreadCountDto>(r)
        return result.ok ? result.data.unreadCount : 0
      } catch (e) {
        normalizeFailure(e)
        return 0
      }
    },
    staleTime: 0,           // always fresh
    refetchInterval: 30_000, // poll every 30s while tab is active
  })
}
```

---

### B.6 useNotificationHub (SignalR)

**New file:** `src/features/notifications/hooks/useNotificationHub.ts`

```typescript
import { useEffect, useRef } from 'react'
import { useQueryClient }    from '@tanstack/react-query'
import * as signalR          from '@microsoft/signalr'
import { queryKeys }         from '@shared/api/queryKeys'

const HUB_URL = '/hubs/notification'

/**
 * Connects to the Notification SignalR hub.
 * On receiving a "NewNotification" push, invalidates the unread count and list queries
 * so the UI refreshes automatically without waiting for the next poll interval.
 */
export function useNotificationHub(userId: number | null) {
  const qc         = useQueryClient()
  const hubRef     = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    if (!userId) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => localStorage.getItem('aizen_access_token') ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    hubRef.current = connection

    connection.on('NewNotification', () => {
      void qc.invalidateQueries({ queryKey: queryKeys.notifications.unreadCount() })
      void qc.invalidateQueries({ queryKey: queryKeys.notifications.all })
    })

    connection
      .start()
      .then(() => connection.invoke('JoinUserGroup', String(userId)))
      .catch((err) => console.warn('[NotificationHub] connection error:', err))

    return () => {
      void connection.stop()
    }
  }, [userId, qc])
}
```

---

### B.7 useMarkNotificationRead

**New file:** `src/features/notifications/hooks/useMarkNotificationRead.ts`

```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query'
import httpClient                      from '@shared/api/httpClient'
import { ENDPOINTS }                   from '@shared/api/endpoints'
import { queryKeys }                   from '@shared/api/queryKeys'
import { normalizeFailure }            from '@shared/api/responseNormalizer'

export function useMarkNotificationRead() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: async (id: number) => {
      try {
        await httpClient.post(ENDPOINTS.NOTIFICATIONS_MARK_READ(id))
      } catch (e) {
        normalizeFailure(e)
      }
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: queryKeys.notifications.unreadCount() })
      void qc.invalidateQueries({ queryKey: queryKeys.notifications.all })
    },
  })
}

export function useMarkAllNotificationsRead() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: async () => {
      try {
        await httpClient.post(ENDPOINTS.NOTIFICATIONS_MARK_ALL_READ)
      } catch (e) {
        normalizeFailure(e)
      }
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: queryKeys.notifications.all })
    },
  })
}
```

---

### B.8 NotificationBell Component

**New file:** `src/shared/ui/notification-bell/NotificationBell.tsx`

```tsx
import { useNotificationUnreadCount } from '@features/notifications/hooks/useNotificationUnreadCount'
import { Icon }                       from '@shared/ui/icon/Icon'

interface Props {
  onClick: () => void
}

/**
 * Drop into the app header. Renders a bell icon with a red badge when there are unread notifications.
 */
export function NotificationBell({ onClick }: Props) {
  const count = useNotificationUnreadCount()
  const unread = count.data ?? 0

  return (
    <button
      onClick={onClick}
      className="relative flex items-center justify-center w-9 h-9 rounded-xl hover:bg-surface-container transition-colors"
      aria-label={`Notifications${unread > 0 ? ` (${unread} unread)` : ''}`}
    >
      <Icon name="notifications" size={22} className="text-on-surface-variant" />
      {unread > 0 && (
        <span className="absolute top-1 right-1 flex h-4 w-4 items-center justify-center rounded-full bg-error text-on-error text-[10px] font-bold leading-none">
          {unread > 99 ? '99+' : unread}
        </span>
      )}
    </button>
  )
}
```

---

### B.9 NotificationsInboxPage

**New file:** `src/pages/app/NotificationsInboxPage.tsx`

```tsx
import { useState }                       from 'react'
import { PageHeader }                     from '@shared/ui/page-header/PageHeader'
import { DashboardCard }                  from '@shared/ui/dashboard-card/DashboardCard'
import { LoadingSkeleton }                from '@shared/ui/loading-skeleton/LoadingSkeleton'
import { EmptyState }                     from '@shared/ui/empty-state/EmptyState'
import { Icon }                           from '@shared/ui/icon/Icon'
import { Pagination }                     from '@shared/ui/pagination/Pagination'
import { useNotificationsQuery }          from '@features/notifications/hooks/useNotificationsQuery'
import { useMarkNotificationRead, useMarkAllNotificationsRead } from '@features/notifications/hooks/useMarkNotificationRead'
import type { InAppNotificationDto }      from '@shared/api/types/notification.types'

// ── Type icon mapping ──────────────────────────────────────────────────────────

const TYPE_ICON: Record<string, string> = {
  CargoDryKitActivated:  'water_drop',
  CargoDryKitExpiring:   'timer',
  CargoDryKitExpired:    'error',
  CargoDryKitRenewed:    'autorenew',
  CargoDryKitRevoked:    'block',
  ServiceRequestCreated: 'engineering',
  OfferCreated:          'local_offer',
  AssignmentCreated:     'assignment_turned_in',
  CompletionSubmitted:   'task_alt',
  DisputeOpened:         'gavel',
  NewMessage:            'chat',
}

function notifIcon(type: string): string {
  return TYPE_ICON[type] ?? 'notifications'
}

// ── Notification Row ───────────────────────────────────────────────────────────

function NotifRow({ notif, onRead }: { notif: InAppNotificationDto; onRead: (id: number) => void }) {
  const isUnread = !notif.isRead

  return (
    <div
      onClick={() => { if (isUnread) onRead(notif.id) }}
      className={`flex gap-3 items-start p-4 border-b border-outline-variant/20 last:border-0 transition-colors
        ${isUnread ? 'bg-primary/5 cursor-pointer hover:bg-primary/10' : 'hover:bg-surface-container/50'}`}
    >
      {/* Icon */}
      <div className={`flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center
        ${isUnread ? 'bg-primary/20' : 'bg-surface-container-high'}`}>
        <Icon name={notifIcon(notif.type)} size={16}
          className={isUnread ? 'text-primary' : 'text-on-surface-variant'} />
      </div>

      {/* Content */}
      <div className="flex-1 min-w-0">
        <p className={`text-body-sm ${isUnread ? 'font-semibold text-on-surface' : 'text-on-surface-variant'}`}>
          {notif.title}
        </p>
        <p className="text-label-sm text-on-surface-variant mt-0.5 line-clamp-2">
          {notif.body}
        </p>
        <p className="text-label-caps text-on-surface-variant/60 mt-1">
          {new Date(notif.createdAt).toLocaleString('en-GB', {
            day: '2-digit', month: 'short', year: 'numeric',
            hour: '2-digit', minute: '2-digit',
          })}
        </p>
      </div>

      {/* Unread indicator */}
      {isUnread && (
        <div className="flex-shrink-0 w-2 h-2 rounded-full bg-primary mt-1.5" />
      )}
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

export function NotificationsInboxPage() {
  const [page, setPage] = useState(1)

  const { data, isLoading } = useNotificationsQuery(page)
  const markRead    = useMarkNotificationRead()
  const markAllRead = useMarkAllNotificationsRead()

  const items      = data?.items ?? []
  const total      = data?.total ?? 0
  const unread     = data?.unreadCount ?? 0
  const totalPages = Math.ceil(total / 20)

  return (
    <div>
      <PageHeader
        title="Notifications"
        subtitle={unread > 0 ? `${unread} unread` : 'All caught up'}
        actions={
          unread > 0 ? (
            <button
              onClick={() => void markAllRead.mutateAsync()}
              disabled={markAllRead.isPending}
              className="flex items-center gap-2 rounded-xl border border-outline-variant/40 px-4 py-2 text-label-sm font-medium text-on-surface-variant hover:bg-surface-container transition-colors disabled:opacity-50"
            >
              <Icon name="done_all" size={16} />
              Mark all read
            </button>
          ) : null
        }
      />

      <DashboardCard title="" subtitle="">
        {isLoading ? (
          <LoadingSkeleton lines={6} />
        ) : items.length === 0 ? (
          <EmptyState
            title="No notifications"
            description="You're all caught up. Notifications will appear here."
          />
        ) : (
          <>
            <div className="divide-y divide-outline-variant/20">
              {items.map((n) => (
                <NotifRow
                  key={n.id}
                  notif={n}
                  onRead={(id) => void markRead.mutateAsync(id)}
                />
              ))}
            </div>

            {totalPages > 1 && (
              <Pagination
                page={page}
                totalPages={totalPages}
                totalItems={total}
                pageSize={20}
                onPageChange={setPage}
              />
            )}
          </>
        )}
      </DashboardCard>
    </div>
  )
}
```

---

### B.10 Register Route

**File:** `src/router/routes.ts` — add:

```typescript
{ path: '/notifications', element: lazy(() => import('@pages/app/NotificationsInboxPage').then(m => ({ default: m.NotificationsInboxPage }))) },
```

**File:** `src/router/routeObjects.tsx` — add to side nav or header link:

```typescript
{
  path: '/notifications',
  label: 'Notifications',
  icon: 'notifications',
},
```

**File:** App header component — wire `NotificationBell`:

```tsx
import { NotificationBell } from '@shared/ui/notification-bell/NotificationBell'
import { useNavigate }      from 'react-router-dom'

const navigate = useNavigate()

// In header JSX:
<NotificationBell onClick={() => navigate('/notifications')} />
```

---

### B.11 Wire useNotificationHub in App Root

**File:** `src/App.tsx` or the authenticated layout component:

```tsx
import { useNotificationHub } from '@features/notifications/hooks/useNotificationHub'
import { useAuthStore }       from '@features/auth/store/authStore'

// Inside the authenticated layout component:
const userId = useAuthStore((s) => s.user?.id ?? null)
useNotificationHub(userId)
```

This runs the SignalR connection for the duration of the authenticated session and automatically invalidates notification queries on push.

---

## PART C — BFF: Notification Endpoints

These endpoints must be added to the AdminPanel BFF to expose the notification inbox to the frontend.

### C.1 Refit Interface

**File:** `Aizen.AdminPanel.BFF.Notification/RemoteCalls/INotificationBffRemoteCall.cs`

```csharp
[Get("/notification/my")]
Task<InAppNotificationListDto> GetMyNotificationsAsync(
    [AliasAs("page")]     int page     = 1,
    [AliasAs("pageSize")] int pageSize = 20,
    CancellationToken cancellationToken = default);

[Get("/notification/my/unread-count")]
Task<NotificationUnreadCountDto> GetUnreadCountAsync(
    CancellationToken cancellationToken = default);

[Post("/notification/my/{id}/read")]
Task MarkReadAsync(
    [AliasAs("id")] long id,
    CancellationToken cancellationToken = default);

[Post("/notification/my/mark-all-read")]
Task MarkAllReadAsync(
    CancellationToken cancellationToken = default);
```

### C.2 BFF Controller Endpoints

**File:** `Aizen.AdminPanel.BFF.Notification/Controllers/NotificationsController.cs`

Route prefix: `/api/v1/notifications`

```csharp
[HttpGet]
[Authorize]
public async Task<IActionResult> GetMyNotifications(
    [FromQuery] int page     = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
{
    var result = await _remote.GetMyNotificationsAsync(page, pageSize, cancellationToken);
    return Ok(AizenResponse.Success(result));
}

[HttpGet("unread-count")]
[Authorize]
public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
{
    var result = await _remote.GetUnreadCountAsync(cancellationToken);
    return Ok(AizenResponse.Success(result));
}

[HttpPost("{id:long}/read")]
[Authorize]
public async Task<IActionResult> MarkRead(long id, CancellationToken cancellationToken)
{
    await _remote.MarkReadAsync(id, cancellationToken);
    return Ok(AizenResponse.Success(true));
}

[HttpPost("mark-all-read")]
[Authorize]
public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
{
    await _remote.MarkAllReadAsync(cancellationToken);
    return Ok(AizenResponse.Success(true));
}
```

---

## STEP D — Execution Order

Execute in this exact order:

1. **notification-module-v1/PROMPT_A** → Domain, EF, Repositories
2. **notification-module-v1/PROMPT_B** → Application: CQRS, Consumers (wire CargoDry consumers from Part A above)
3. **notification-module-v1/PROMPT_C** → API: SignalR Hub, Controllers, Program.cs
4. **Part A above** → Add CargoDry consumer implementations + seed templates
5. **Part B above** → Frontend: types, hooks, bell, page, routes
6. **Part C above** → BFF notification controller

---

## STEP E — Validation

**Backend:**
- [ ] `dotnet build` — zero errors across Notification.Abstraction, .Domain, .Repository, .Application, .Api
- [ ] `POST /notification/send` (internal) creates a `UserNotification` record in `notification.user_notifications`
- [ ] `GET /notification/my` returns paginated list for authenticated user
- [ ] `GET /notification/my/unread-count` returns `{ unreadCount: N }`
- [ ] `POST /notification/my/{id}/read` marks notification read and updates `ReadAt`
- [ ] SignalR hub `/hubs/notification` accepts connections (JWT auth)
- [ ] `JoinUserGroup` adds connection to `user:{userId}` group
- [ ] CargoDry consumers registered in MassTransit — verify via RabbitMQ management UI or MassTransit health check

**Frontend:**
- [ ] `tsc --noEmit` — zero errors
- [ ] `NotificationBell` renders in app header with correct unread badge count
- [ ] Clicking bell navigates to `/notifications`
- [ ] `NotificationsInboxPage` renders list with unread items highlighted
- [ ] Clicking an unread notification calls `mark-read` and removes highlight
- [ ] "Mark all read" button appears only when `unreadCount > 0`
- [ ] After activating a CargoDry kit (via test), a notification appears in the inbox within 30s (poll cycle)
- [ ] SignalR push delivers "NewNotification" event instantly and refreshes badge without page reload

---

## Key Constraints

| Concern | Rule |
|---|---|
| SignalR auth | JWT access token passed via `accessTokenFactory` — do NOT use cookie auth for the hub |
| Access token key | `'aizen_access_token'` in localStorage — match what `httpClient` uses for Bearer header |
| Hub group naming | Exact format: `user:{userId}` (long as string) — must match `JoinUserGroup` in hub |
| Poll interval | 30 seconds for unread count — reduce to 10s only if UX requirements demand it |
| Template rendering | All `{{variable}}` substitution is done server-side in `TemplateInterpolatorService` |
| Notification types | CargoDry range: 300–399 as defined in `NotificationType` enum (PROMPT_A) |
| Consumer pattern | `ExecutePrepareMessage` → immediate true; all logic in `ExecuteCommitMessage` |
