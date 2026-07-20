# Notification list `Total` must be the grand total (fixes infinite scroll) — Backend Prompt

> **Context:** Inktavia Marine OS, Notification module. The provider notification dropdown uses infinite scroll
> (`useInfiniteQuery`, first page take=10 then take=5) and stops paging via `loaded < total`. It never pages past the
> first 10 because the API returns a wrong `Total`.

## Root cause (verified on the wire)
`GetUserNotificationsResponse` / `NotificationListResponse.Total` is currently set to the **current page size**
(`Total = items.Count`) in `GetUserNotificationsQueryHandler`, not the recipient's grand total.
- `GET ...?take=30` → items=28, total=28 (looks fine only because the page holds everything).
- `GET ...?take=10` → items=10, **total=10** → the FE computes `loaded(10) < total(10)` = false → `hasNextPage=false` →
  no more pages load.

`Total` must be the **total number of notifications for the recipient**, independent of `skip`/`take`.

## Fix

1. **Repository** — `INotificationRepository` + `NotificationRepository`: add a count method (mirror
   `GetUnreadCountAsync`, but without the `ReadAt == null` filter):
   ```csharp
   Task<int> CountByRecipientAsync(long userId, CancellationToken ct);
   // impl:
   public Task<int> CountByRecipientAsync(long userId, CancellationToken ct)
       => _db.Notifications.CountAsync(x => x.RecipientUserId == userId, ct);
   ```

2. **Handler** — `GetUserNotificationsQueryHandler`: set `Total` from the new count, NOT `items.Count`:
   ```csharp
   var items = await _repository.GetByRecipientAsync(rid, request.Skip, request.Take, ct);
   var total = await _repository.CountByRecipientAsync(rid, ct);
   var unread = await _repository.GetUnreadCountAsync(rid, ct);
   // ... map items -> NotificationDto ...
   return new NotificationListResponse { Items = mapped, Total = total, UnreadCount = unread };
   ```
   Keep the existing Redis cache wrapper as-is (the cached `NotificationListResponse` now carries the correct `Total`;
   generation-based invalidation on writes already refreshes it).

> No BFF or FE changes. The FE already stops at `loaded >= total`.

## Verify — run and PASTE output (container + HTTP; do not report done until all pass)
provider2 = **100011**. DB: aizen/aizen. Expected total ≈ **28**.

1. Build Notification: 0 errors. Rebuild + restart `notification-api`; clean boot.
2. HTTP smoke (provider2 token):
   ```
   GET /api/v1/provider/notifications?skip=0&take=10   # body.items=10, body.total=28  (NOT 10)
   GET /api/v1/provider/notifications?skip=10&take=5    # body.items=5,  body.total=28
   GET /api/v1/provider/notifications?skip=25&take=5    # body.items=3,  body.total=28  (last partial page)
   ```
   `total` must equal the recipient's full count (~28) on every page, regardless of `take`.
3. DB cross-check:
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT COUNT(*) FROM notifications WHERE \"RecipientUserId\"=100011;"   -- matches body.total
   ```

## Acceptance
- `NotificationListResponse.Total` = recipient grand total on every page (take-independent); `UnreadCount` unchanged.
- Infinite scroll now advances (10 → 15 → 20 → … → total) and stops at the end.
- Build clean; container Up; HTTP + DB evidence pasted.

## Report
`REPORT_BACKEND.md` ("Notification Total count fix"): added `CountByRecipientAsync`; handler now sets `Total` to the
recipient grand total instead of the page size. Verified via HTTP (total=28 across pages) + DB.
