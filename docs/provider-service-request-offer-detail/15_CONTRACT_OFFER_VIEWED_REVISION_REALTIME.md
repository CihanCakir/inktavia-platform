# 15 — Contract: Offer *Viewed* & *Revision Requested* realtime (customer → provider)

**Amaç (TR):** Sağlayıcı portalındaki tek açık kalan realtime bağımlılık, müşteri (mobil) tarafında henüz olmayan
iki olaya bağlı: müşteri teklifi **gördü** ve müşteri **revizyon istedi**. Bu doküman, 2 hafta sonra mobil
ekranların backend'i bağlanırken **iki ekibin de aynı sözleşmeye** göre kod yazması için event adlarını,
payload'ları, komutları ve consumer'ları donduruyor. Uydurma yok — hepsi mevcut `OfferAccepted` akışının aynısını
takip ediyor.

> Bu bir **sözleşme dondurma** dokümanıdır. Önce mesaj kontratları + event sabitleri paylaşılır; sonra müşteri
> tarafı (yayıncı) ve sağlayıcı tarafı (tüketici) bu kontrata karşı **paralel** yazılabilir.

---

## 0. Reference pattern (already in the codebase — copy it)

The **OfferAccepted** flow is the exact template. Do not invent a new mechanism; mirror this:

```
[customer action]
  AcceptServiceRequestOfferCommandHandler        (Modules/ServiceRequest, owner-scoped)
    → offer.Accept(); repo.Update()
    → _messagePublisher.PublishAsync(new ServiceRequestOfferAcceptedMessage {          // RabbitMQ
          ServiceRequestId, OfferId, OwnerUserId, ProviderProfileId })
                                   │
                                   ▼  (competing consumer, Redis backplane, N replicas)
  OfferAcceptedRealtimeConsumer : AizenBaseMessageConsumer<ServiceRequestOfferAcceptedMessage>
    (Bff/MarineProvider/Realtime)
    → group = ProviderRealtimeHub.ProviderGroup(message.ProviderProfileId)  // "provider:{id}"
    → _hub.Clients.Group(group).SendAsync("providerEvent", new ProviderRealtimeEvent {
          EventType = ProviderRealtimeEventTypes.OfferAccepted, ServiceRequestId, OfferId })
                                   │
                                   ▼
[provider SPA]  onProviderEvent(evt) → toast + invalidate queryKeys.serviceRequests.*
```

Two things are **already reserved** on the provider side and must be reused verbatim:

- `Bff/…/Realtime/ProviderRealtimeEvent.cs` → `ProviderRealtimeEventTypes.OfferViewedByCustomer = "OfferViewedByCustomer"`
  and `OfferRevisionRequested = "OfferRevisionRequested"`. **Constants exist; no consumer yet.**
- `Modules/ServiceRequest/…/Offer/ServiceRequestOfferEntity.cs` →
  `MarkViewed(DateTime utcNow)` (`ViewedAt ??= utcNow;` — first-view-wins, idempotent) and
  `MarkRevisionRequested(DateTime utcNow)` (`RevisionRequestedAt = utcNow;`). **Both are currently dead code —
  called from nowhere.** Persisted fields `ViewedAt`, `RevisionRequestedAt` already exist on the table.

So the work is: wire a customer command to each dead method, publish a message, add the mirrored provider consumer.

---

## 1. Integration message contracts (FREEZE FIRST — shared abstraction)

Location: `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Abstraction/Message/`. Mirror
`ServiceRequestOfferAcceptedMessage` exactly (`: AizenBaseMessage`, plain settable props).

```csharp
// ServiceRequestOfferViewedMessage.cs
public sealed class ServiceRequestOfferViewedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long OfferId { get; set; }
    public long OwnerUserId { get; set; }         // who viewed (the owner)
    public long ProviderProfileId { get; set; }   // REQUIRED — the BFF addresses provider:{this}
    public DateTime ViewedAtUtc { get; set; }
}

// ServiceRequestOfferRevisionRequestedMessage.cs
public sealed class ServiceRequestOfferRevisionRequestedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long OfferId { get; set; }
    public long OwnerUserId { get; set; }
    public long ProviderProfileId { get; set; }   // REQUIRED — provider group addressing
    public DateTime RequestedAtUtc { get; set; }
    // NOTE is intentionally NOT on the realtime frame (see §4). If needed on the bus for
    // Notification email/push, it MAY live here — but must never be forwarded into ProviderRealtimeEvent.
    public string? Note { get; set; }
}
```

**Why `ProviderProfileId` is mandatory:** the provider group is server-addressed from the message
(`ProviderGroup(message.ProviderProfileId)`). The customer app never sends, and the provider BFF never trusts, a
client-supplied provider id. The publisher reads it off the offer row (`offer.ProviderProfileId`).

---

## 2. Customer/module side — publishers (mobile team, ~2 weeks)

Two owner-scoped commands in `Modules/ServiceRequest/…/Application/Command/Offer/`, dispatched from the customer
BFF. Mirror `AcceptServiceRequestOfferCommandHandler` (load SR+offer, **verify the caller is the SR owner**,
mutate domain, `repo.Update()`, then `_messagePublisher.PublishAsync(...)`).

### 2a. `MarkOfferViewedCommand(serviceRequestId, offerId)`
- Load the offer; guard: it belongs to `serviceRequestId`, the SR's `OwnerUserId == currentUserId`, and the offer
  is in a customer-visible status (`Submitted`/`UnderReview`/`Accepted`/`Rejected` — a `Draft` is never visible to
  the customer, so viewing one is a bug → reject).
- **Publish only on the null→set transition.** `if (offer.ViewedAt is null) { offer.MarkViewed(utcNow);
  repo.Update(); publish ServiceRequestOfferViewedMessage; }` Subsequent opens are a no-op and publish nothing —
  the provider must not get a "viewed" toast every time the customer reopens the offer.
- No status change. Viewing is passive.

### 2b. `RequestOfferRevisionCommand(serviceRequestId, offerId, note)`
- Same owner guard. Offer must be `Submitted` (or `UnderReview`) — you cannot request a revision on a `Draft`,
  `Accepted`, `Withdrawn`, or `Rejected` offer.
- Stamp `offer.MarkRevisionRequested(utcNow)` **and** apply the status decision in §3.
- `repo.Update()`, then publish `ServiceRequestOfferRevisionRequestedMessage` (carry `Note` for Notification).
- Multiple revision requests over time are legitimate (unlike viewed) → each publishes.

### Controller (customer, module) — mirror accept/reject on `ServiceRequestOfferController`
```
PATCH api/v1/service-requests/{serviceRequestId:long}/offers/{offerId:long}/view
PATCH api/v1/service-requests/{serviceRequestId:long}/offers/{offerId:long}/request-revision   body: { "note": "…" }
```
The customer/mobile BFF proxies to these; owner identity is resolved there (the same way the provider BFF resolves
provider identity for its calls). **Not** the provider BFF — these are customer actions.

---

## 3. DECISION (confirm before building): does *revision requested* reopen the offer for editing?

`MarkRevisionRequested` today only stamps a timestamp; it does **not** change `Status`. But the provider read page
treats `Draft` as editable and `Submitted/UnderReview/Accepted` as read-only, and `offer.Update()`/`SaveOfferDraft`
are **Draft-only**. So if a revision is requested, the provider currently **cannot edit** to answer it. Pick one:

| Option | Mechanics | Cost | Trade-off |
|---|---|---|---|
| **A — reopen to Draft (recommended)** | Add `offer.ReopenForRevision(utcNow)`: guard `Submitted/UnderReview` → `Status = Draft`, set `RevisionRequestedAt`. Provider edits via the existing Draft path and re-submits (`MarkSubmitted` already requires Draft). | Low — reuses every existing guard & the Submit path. | Status momentarily leaves "Submitted"; history preserved by `SubmittedAt` + `RevisionRequestedAt` + status-history rows. |
| B — new `RevisionRequested` status | Add enum value; update every switch/label/i18n and the read-page editable check. | High — touches enum consumers across module + BFF + FE. | Most explicit lifecycle. |
| C — keep `Submitted`, add provider "reopen" action | Provider clicks "Revize et" → server transitions Submitted→Draft on demand. | Medium — extra provider endpoint + UX step. | Two-step; provider controls when it reopens. |

**Recommendation: Option A.** Safest, architecture-compatible, no enum churn. `RequestOfferRevisionCommand` calls
`ReopenForRevision`; the provider sees a "Müşteri revizyon istedi" badge (from `RevisionRequestedAt`) on a now
editable builder and re-submits. Add the domain method:

```csharp
public void ReopenForRevision(DateTime utcNow)
{
    if (Status is not (ServiceRequestOfferStatus.Submitted or ServiceRequestOfferStatus.UnderReview))
        throw new InvalidOperationException($"Cannot request revision on an offer in status {Status}.");
    Status = ServiceRequestOfferStatus.Draft;
    RevisionRequestedAt = utcNow;
}
```

*(If you prefer B or C, say so and this doc's §2b + provider §5 change accordingly.)*

---

## 4. Provider side — consumers (our team, when messages land)

Two consumers in `Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Realtime/`, **copies** of
`OfferAcceptedRealtimeConsumer` with the type + event-constant swapped. Nothing else differs.

```csharp
// OfferViewedRealtimeConsumer : AizenBaseMessageConsumer<ServiceRequestOfferViewedMessage>
//   guard message.ProviderProfileId > 0
//   group = ProviderRealtimeHub.ProviderGroup(message.ProviderProfileId)
//   SendAsync("providerEvent", new ProviderRealtimeEvent {
//       EventType = ProviderRealtimeEventTypes.OfferViewedByCustomer,
//       ServiceRequestId = message.ServiceRequestId, OfferId = message.OfferId })

// OfferRevisionRequestedRealtimeConsumer : AizenBaseMessageConsumer<ServiceRequestOfferRevisionRequestedMessage>
//   … EventType = ProviderRealtimeEventTypes.OfferRevisionRequested …
```

**The revision `Note` never goes on the realtime frame.** `ProviderRealtimeEvent` is deliberately "nothing
sensitive; the client refetches for the truth." A customer-authored note is free text → it stays off the socket;
the provider SPA refetches the detail to read it. (This matches the existing `OfferAccepted` design comment.)

**Security (same as OfferAccepted/Rejected):** these are per-provider events and must reach **only** that
provider's group. Addressing is server-side from `ProviderProfileId`. A provider learning that a *competitor's*
offer was viewed/revised is a commercial leak.

**Idempotency / multi-replica:** competing consumers on a Redis backplane, at-least-once delivery. The realtime
push is a hint; the client invalidates + refetches, so a duplicate frame is at worst a duplicate toast. `viewed`
is published once (null→set), so duplicates there are only redelivery, not re-views.

---

## 5. Provider follow-ups (our team, Phase C — small)

1. **Detail DTO:** provider `MyOffer` detail response does not yet carry `viewedAt` / `revisionRequestedAt`. Add
   both so the read page can render "Görüldü {relative}" and "Revizyon istendi" and (Option A) show the builder as
   editable again. (`detailTypes.ts MyOffer` + the BFF `GetServiceRequestDetail` mapping.)
2. **SPA realtime handler:** handle the two new `providerEvent` types → toast (`i18n`) + invalidate
   `queryKeys.serviceRequests.detail(id)` and the list. The socket wiring already exists; this is two `case`s.
3. **UX (Option A):** when `revisionRequestedAt > submittedAt`, reopen `OfferBuilder` with a "revizyon istendi"
   banner; re-submit uses the existing submit path.

---

## 6. Notification module (note, not blocking)

- **Viewed** → realtime hint only. **Do not** raise a push/email — too noisy.
- **RevisionRequested** → actionable; it *should* also produce a Notification (push/email) alongside the realtime,
  the same way `OfferAccepted` fans out to Notification. Carry `Note` on the message for that (§1).

---

## 7. Build order & acceptance

**Freeze (day 0):** §1 message contracts committed to the shared abstraction; §0 event constants already present.
Both teams now compile against them.

**Mobile/customer backend (~2 weeks):** §2 commands + controller endpoints + §3 `ReopenForRevision`. Acceptance:
- Owner opens a submitted offer → `ServiceRequestOfferViewedMessage` on the bus **once**; reopening → no new message.
- Owner requests revision (with note) → offer `Status=Draft`, `RevisionRequestedAt` set,
  `ServiceRequestOfferRevisionRequestedMessage` on the bus (note included). Non-owner → rejected. Draft/Accepted →
  rejected.

**Provider backend (our team):** §4 consumers + §5 DTO fields. Acceptance:
- Publishing each message pushes exactly one `providerEvent` to `provider:{ProviderProfileId}` and to **no other**
  provider group. Note is absent from the frame. Provider SPA toasts + refetches; a revised offer is editable again.

**End-to-end (browser + mobile):** owner views on mobile → provider portal toasts "Teklifiniz görüldü" and the
offer shows "Görüldü". Owner requests revision → provider toasts "Revizyon istendi", the builder reopens.

---

## 8. Out of scope (unchanged)

`message-added` provider realtime (no provider-scoped message event yet) remains a separate, later dependency —
not part of this contract.
