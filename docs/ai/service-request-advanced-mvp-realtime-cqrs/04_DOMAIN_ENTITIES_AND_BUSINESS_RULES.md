# Inktavia Marine OS - ServiceRequest Advanced MVP Realtime CQRS

Use this prompt with GitHub Copilot Agent inside the repository.

All generated implementation must be in English and must follow the existing Aizen/Inktavia architecture.

## Step 04 - Create Domain entities and business rules

Implement ServiceRequest domain model according to existing DDD/base entity conventions.

## Mandatory entities

Implement all first-phase entities:

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

## Suggested entity responsibilities

### ServiceRequestEntity

Main aggregate root for request lifecycle.

Core fields:

```text
Id
VesselId
OwnerUserId
OwnerProfileId if current architecture uses profile-specific ownership
ServiceCategoryId
ServiceTypeId
Title
Description
Priority
Status
LocationSnapshot fields or owned value object
RequestedStartAt
RequestedEndAt
AcceptedOfferId
AssignedProviderProfileId
CurrencyCode
EstimatedTotalAmount
FinalTotalAmount
CreatedAt/CreatedBy/ModifiedAt/ModifiedBy/IsDeleted from base entity if available
```

Relationships:

```text
Items
Offers
Attachments
Messages
Assignments
WorkLogs
Completions
Disputes
StatusHistories
```

### ServiceRequestItemEntity

Represents requested service/product/installation/delivery/labor item.

### ServiceRequestOfferEntity

Provider's offer for a request. Only one active accepted offer is allowed per request.

### ServiceRequestOfferItemEntity

Item-level pricing for service, product, installation, delivery, labor, discount, etc.

### ServiceRequestStatusHistoryEntity

Tracks every status transition with actor and reason.

### ServiceRequestAttachmentEntity

References FileStorage by `FileId`. Do not store binary content.

### ServiceRequestMessageEntity

Scoped message between owner, provider, admin, and system.

### ServiceRequestAssignmentEntity

Provider/team assignment and schedule state.

### ServiceRequestWorkLogEntity

Operational execution log with optional attachments and optional location metadata.

### ServiceRequestCompletionEntity

Completion submission, evidence, owner approval/rejection.

### ServiceRequestDisputeEntity

Dispute lifecycle and admin resolution.

## Business rules

Implement rules in entities/domain services/application services according to existing module style.

Minimum rules:

- Only vessel owner or authorized admin can create a request for a vessel.
- A request must have a valid vessel.
- A request must have a valid service category/type from ReferenceData.
- A request cannot accept more than one active offer.
- Withdrawn/rejected/expired offers cannot be accepted.
- Offer acceptance should move request status to `OfferAccepted` or `WaitingForAssignment` according to the flow.
- Assignment can only be created after an offer is accepted unless admin override is explicitly supported.
- Only provider/admin can add work logs after assignment.
- Completion can only be submitted when work is in progress or assignment is completed.
- Owner approval can only happen for submitted completion.
- Dispute can be opened by owner/provider/admin depending on status and access rules.
- Status transitions must be written to `ServiceRequestStatusHistoryEntity`.
- Attachment records must reference valid FileStorage `FileId` values.
- Soft deleted records must not appear in normal queries.

## Domain events

Create domain/integration event types if existing modules use them. Minimum logical events:

```text
ServiceRequestCreatedDomainEvent
ServiceRequestStatusChangedDomainEvent
ServiceRequestOfferCreatedDomainEvent
ServiceRequestOfferAcceptedDomainEvent
ServiceRequestAssignedDomainEvent
ServiceRequestMessageSentDomainEvent
ServiceRequestWorkLogAddedDomainEvent
ServiceRequestCompletionSubmittedDomainEvent
ServiceRequestDisputeOpenedDomainEvent
ServiceRequestDisputeResolvedDomainEvent
```

These events can be used by realtime publisher, Notification later, Payment later, and audit/outbox later.

## Documentation

Every public class/interface must include `DocumentationInfo` if the project uses this pattern.

## Output required

Create/update:

```text
ai/service-request-advanced-mvp-realtime-cqrs/reports/04_DOMAIN_REPORT.md
```
