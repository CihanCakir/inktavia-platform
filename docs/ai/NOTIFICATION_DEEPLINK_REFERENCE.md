# Notification Deep-Link Reference (typed) — Backend Prompt

> **Context:** Inktavia Marine OS, `Aizen.Modules.Notification`. The provider notification list/dropdown should let a
> user click a notification and jump to the related entity (service request, job, message thread, CargoDry, payment).
> Today `MetadataJson` carries free-form data inconsistently. This adds a **typed, routable reference**
> (`ReferenceType` + `ReferenceId`) that consumers populate and the FE maps to a route. No free-form JSON parsing on
> the client.

## 1) Entity + migration
Add to **`NotificationEntity`** (`Domain/Entities/NotificationEntity.cs`):
```csharp
/// <summary>Routable entity kind for deep-link (e.g. "ServiceRequest","Job","Message","CargoDry","Payment","Milestone"). Nullable.</summary>
public string? ReferenceType { get; private set; }
/// <summary>Id of the referenced entity for deep-link. Nullable.</summary>
public long?   ReferenceId   { get; private set; }
```
- Set them in the `Create(...)` factory (add two optional params, default null) and in `CreateSeed(...)`.
- EF config: `ReferenceType` varchar(40) nullable, `ReferenceId` bigint nullable.
- **Migration** `AddNotificationReference` (auto-applied on boot). Existing rows → null (safe; FE falls back to no deep-link).

## 2) DTO
`Notification.Abstraction/Dto/NotificationDto.cs` — add:
```csharp
public string? ReferenceType { get; init; }
public long?   ReferenceId   { get; init; }
```
Map them wherever `NotificationDto` is built (the `GetUserNotificationsQueryHandler` mapping).

## 3) Command plumbing
`SendNotificationCommand` — add optional `string? ReferenceType`, `long? ReferenceId`. `SendNotificationCommandHandler`
passes them to `NotificationEntity.Create(...)`.

## 4) Populate from consumers (provider-relevant)
Set the reference when constructing `SendNotificationCommand` in these consumers (the message payloads already carry the ids):
- **ServiceRequest** (`ServiceRequest*Consumer`): `ReferenceType="ServiceRequest"`, `ReferenceId=serviceRequestId`.
  For offer/assignment events keep `ServiceRequest`+the request id (the FE routes to the request/job).
- **CargoDry kit** (`CargoDryKit*Consumer`): `ReferenceType="CargoDry"`, `ReferenceId=kitId` (FE routes to the CargoDry section).
- **CargoDry milestone** (`CargoDryProviderMilestoneReachedConsumer`): `ReferenceType="Milestone"`, `ReferenceId=0`/null
  (FE routes to the CargoDry earnings cockpit; no per-entity id needed).
- **Messaging** (`MessagingMessageSentConsumer`): `ReferenceType="Message"`, `ReferenceId=conversationId`.
- **Payment/Payout** (`Payment*Consumer`,`PayoutCompletedConsumer`): `ReferenceType="Payment"`, `ReferenceId=<paymentId/settlementId>`.
> Scope to these; other consumers can adopt the same two fields later. InApp + Push both inherit (same command).

## 5) Display seed (for testable deep-link now)
Update `NotificationDisplayMockSeed`: give each seeded row a realistic reference so the FE deep-link is verifiable —
e.g. ServiceRequest rows → `("ServiceRequest", <someId>)`, Kit rows → `("CargoDry", <kitId>)`, milestone rows →
`("Milestone", null)`. Keep the existing idempotent `SEED_DISPLAY` guard.

---

## Verify — run and PASTE output (container + DB; do not report done until all pass)
DB: aizen/aizen. provider2 = **100011**.

1. **Build** Notification: 0 errors. Rebuild + restart `notification-api`; migration `AddNotificationReference` applied; clean boot.
2. **Schema:**
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "\d notifications" | grep -iE "ReferenceType|ReferenceId"
   ```
3. **Seed carries references:**
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT \"Type\",\"ReferenceType\",\"ReferenceId\", COUNT(*)
       FROM notifications WHERE \"RecipientUserId\"=100011
      GROUP BY 1,2,3 ORDER BY 1;"
   -- expect non-null ReferenceType for routable types (ServiceRequest/CargoDry/Message/...); milestones ReferenceType='Milestone'
   ```
4. **DTO exposes them (BFF, provider2 token):**
   ```
   GET /api/v1/provider/notifications?skip=0&take=10
   # each item includes referenceType / referenceId (null allowed for non-routable)
   ```
5. **New notification path:** trigger one real provider notification (e.g. a CargoDry milestone) → its row has the
   expected `ReferenceType`/`ReferenceId` in DB.

## Acceptance
- `notifications.ReferenceType` + `ReferenceId` columns exist (migration applied); DTO exposes them.
- Provider-relevant consumers + the display seed populate them; existing rows null-safe.
- `GET /provider/notifications` returns `referenceType`/`referenceId`; DB matches. Build clean; evidence pasted.

## Report
`REPORT_BACKEND.md` ("Notification deep-link reference"): added typed `ReferenceType`/`ReferenceId` (+migration), DTO +
`SendNotificationCommand` plumbing, populated in provider-relevant consumers + display seed. Verified via schema + DB +
GET. FE maps referenceType→route.
