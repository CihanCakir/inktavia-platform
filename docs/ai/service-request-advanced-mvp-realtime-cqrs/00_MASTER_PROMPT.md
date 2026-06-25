# Inktavia Marine OS - ServiceRequest Advanced MVP Realtime CQRS

Use this prompt with GitHub Copilot Agent inside the repository.

All generated implementation must be in English and must follow the existing Aizen/Inktavia architecture.

## Master objective

Create the `ServiceRequest` module as an advanced MVP realtime CQRS module for Inktavia Marine OS.

This module must implement the complete first-phase operational lifecycle:

```text
Boat Owner -> Vessel -> ServiceRequest -> Provider Offer -> Assignment -> WorkLog -> Completion -> Owner Approval / Dispute -> Admin Operations
```

## Mandatory first-phase scope

Implement and wire all of the following entities and related flows:

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

## Mandatory module projects/layers

Create or complete these logical layers according to the existing repository layout:

```text
Aizen.Modules.ServiceRequest.Api
Aizen.Modules.ServiceRequest.Application
Aizen.Modules.ServiceRequest.Abstraction
Aizen.Modules.ServiceRequest.Domain
Aizen.Modules.ServiceRequest.Repository
```

If the physical path pattern is different, use the actual repository convention.

## Mandatory realtime requirement

Before creating realtime code, inspect:

```text
Core/Realtime/src/Aizen.Core.Realtime
```

Understand how the existing framework handles:

- SignalR hubs
- Hub base classes
- realtime services/publishers
- group management
- current user/claims extraction
- connection lifecycle
- DI registration
- authentication/authorization
- serialization
- endpoint mapping

Reuse the framework. Do not create an unrelated SignalR implementation.

## Business flows to implement

### 1. Request management

Boat Owner creates a service request for a vessel. The request may include service category/type, priority, description, requested time range, location snapshot, items, and attachments.

### 2. Offer management

Provider creates, updates, withdraws, and manages offers. Owner can accept or reject offers. Offers contain item-level pricing.

### 3. Assignment management

After an offer is accepted, the request can be assigned to a provider profile and optionally a provider team member. Assignment can be scheduled, accepted, rejected, started, completed, or cancelled.

### 4. Realtime communication

Publish realtime events for request, offer, assignment, messages, work logs, completion, and dispute changes.

### 5. Messaging

Allow owner, provider, and admin to exchange messages scoped to a service request.

### 6. Work execution

Provider can add work logs with type, title, description, location metadata, and attachments.

### 7. Completion flow

Provider submits completion evidence. Owner can approve or reject completion. Rejection can open or lead to dispute flow.

### 8. Dispute flow

Owner, provider, or admin can open and manage a dispute. Admin can change status and resolve the dispute.

### 9. Admin operations

Admin can list, inspect, supervise, filter, and act on requests, delayed requests, pending completions, and disputes.

## Required realtime event names

Implement event types and publishing for:

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

## Suggested realtime group model

Adapt to the existing Aizen realtime conventions:

```text
servicerequest:{serviceRequestId}
owner:{ownerUserId}
provider:{providerProfileId}
admin:operations
vessel:{vesselId}
```

## Standards

- Use CQRS command/query handlers.
- Use typed DTO/response results only.
- Do not return `object` from handlers.
- Use request models for API input.
- Use DTO models for API output.
- Use validators for commands/requests.
- Use repository services where the existing architecture does so.
- Use PostgreSQL for main persistence.
- Use Redis/query cache patterns for query optimization.
- Use optional Mongo read/activity documents only if consistent with current architecture.
- Use `DocumentationInfo` on public classes/interfaces/members where required.
- Use current user/client/device context through existing Aizen info accessor pattern.
- Respect authorization rules for owner, provider, and admin.
- Add DI registration and project references.
- Add build validation.
- Produce a final report with implemented files, flows, and any gaps.

## Do not implement these modules inside ServiceRequest

Do not implement Payment, Notification, Review, Commerce, GeoDiscovery, or CargoDry modules inside ServiceRequest. Only create clean integration points/events/contracts where needed.

## Execution

Proceed step by step. Start by analyzing the existing architecture and realtime framework. Then implement the module incrementally. After each major step, ensure the solution still builds or document what remains incomplete and why.
