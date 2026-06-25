# ServiceRequest Advanced MVP Realtime CQRS Guide

## Module position

`ServiceRequest` is the first real operational workflow module after Identity, ReferenceData, Vessel, FileStorage, and minimum ProviderOperations.

It connects the platform's core marketplace loop:

```text
Boat Owner
  -> selects Vessel
  -> creates ServiceRequest
  -> receives Provider Offers
  -> accepts one Offer
  -> Provider/Team is assigned
  -> Work execution is tracked through WorkLogs
  -> Completion evidence is submitted
  -> Owner approves or opens Dispute
  -> Admin can supervise and resolve operational issues
```

## First-phase entities

Implement all of the following in the first phase:

```text
ServiceRequestEntity
ServiceRequestItemEntity
ServiceRequestOfferEntity
ServiceRequestOfferItemEntity
ServiceRequestStatusHistoryEntity
ServiceRequestAttachmentEntity
ServiceRequestMessageEntity
ServiceRequestAssignmentEntity
ServiceRequestWorkLogEntity
ServiceRequestCompletionEntity
ServiceRequestDisputeEntity
```

## Mandatory realtime capability

Use existing `Aizen.Core.Realtime` framework located at:

```text
Core/Realtime/src/Aizen.Core.Realtime
```

Expected realtime events:

```text
ServiceRequestCreated
ServiceRequestUpdated
ServiceRequestStatusChanged
OfferCreated
OfferUpdated
OfferAccepted
OfferRejected
AssignmentCreated
AssignmentUpdated
AssignmentAccepted
AssignmentRejected
MessageSent
WorkLogAdded
WorkStarted
WorkPaused
WorkResumed
CompletionSubmitted
CompletionApproved
CompletionRejected
DisputeOpened
DisputeStatusChanged
DisputeResolved
AdminInterventionRequired
```

Expected group names, adapted to existing realtime conventions:

```text
servicerequest:{serviceRequestId}
owner:{ownerUserId}
provider:{providerProfileId}
admin:operations
vessel:{vesselId}
```

## Cross-module dependencies

ServiceRequest should integrate with or prepare integration points for:

- Identity: user/profile/role/current user validation
- ReferenceData: service categories, service types, currency, unit, location reference validation
- Vessel: vessel existence and ownership/access validation
- FileStorage: attachments/photos/documents by `FileId`
- ProviderOperations: provider profile, service categories, service area, team members
- Payment: later checkout after accepted offer
- Notification: later push/email/SMS fallback for offline users
- Review: later rating after completion
- GeoDiscovery: later nearby provider discovery
- CargoDry: installation/service/renewal related service items

## Persistence

Use PostgreSQL as the main source of truth. Use Mongo only if the current architecture supports optional read/activity documents and it adds value. Use Redis/cache patterns for query optimization.

## Important implementation boundaries

Do not implement Payment, Notification, Review, Commerce, or GeoDiscovery inside ServiceRequest. Only prepare clear integration events/contracts where needed.
