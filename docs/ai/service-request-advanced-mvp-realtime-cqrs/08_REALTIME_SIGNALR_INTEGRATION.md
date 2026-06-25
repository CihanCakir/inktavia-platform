# Inktavia Marine OS - ServiceRequest Advanced MVP Realtime CQRS

Use this prompt with GitHub Copilot Agent inside the repository.

All generated implementation must be in English and must follow the existing Aizen/Inktavia architecture.

## Step 08 - Realtime SignalR integration using Aizen.Core.Realtime

Implement ServiceRequest realtime integration by reusing the existing framework at:

```text
Core/Realtime/src/Aizen.Core.Realtime
```

## Mandatory first action

Re-open and inspect `Aizen.Core.Realtime` before coding this step. Use its existing abstractions and conventions.

## Required ServiceRequest realtime components

Create module-level components only if they fit the framework:

```text
IServiceRequestRealtimePublisher
ServiceRequestRealtimePublisher
ServiceRequestRealtimeEventDto
ServiceRequestRealtimeGroupNames
ServiceRequestRealtimeEventType
ServiceRequestHub or module endpoint mapping only if required by framework
ServiceRequestRealtimeAuthorizationService if needed
```

If the framework already provides generic hub and publisher contracts, implement ServiceRequest-specific adapter/service over them instead of creating a new hub.

## Required realtime events

Publish events for:

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

## Group model

Adapt names to existing framework standards. Logical groups:

```text
servicerequest:{serviceRequestId}
owner:{ownerUserId}
provider:{providerProfileId}
admin:operations
vessel:{vesselId}
```

## Authorization rules

A connection can join a service request group only if the user is one of:

- request owner
- provider assigned to request
- provider who submitted an active/visible offer if business allows offer-scoped access
- admin/operator

A connection can join provider group only if the user belongs to that provider profile/team.

A connection can join admin group only if user has admin/operator role.

## Publisher methods

Implement logically equivalent methods:

```text
PublishToServiceRequestAsync(serviceRequestId, event)
PublishToOwnerAsync(ownerUserId, event)
PublishToProviderAsync(providerProfileId, event)
PublishToAdminOperationsAsync(event)
PublishToVesselAsync(vesselId, event)
PublishOfferCreatedAsync(...)
PublishAssignmentCreatedAsync(...)
PublishMessageSentAsync(...)
PublishWorkLogAddedAsync(...)
PublishCompletionSubmittedAsync(...)
PublishDisputeOpenedAsync(...)
```

Use current framework method names and contracts.

## Command handler integration

Wire realtime publishing after successful persistence/commit in command handlers.

Do not publish events before the transaction succeeds.

## Offline users

Do not implement Notification module here. Add clean TODO/integration event notes for later offline push/email/SMS fallback.

## Output required

Create/update:

```text
ai/service-request-advanced-mvp-realtime-cqrs/reports/08_REALTIME_INTEGRATION_REPORT.md
```

Include:

- what was found in `Aizen.Core.Realtime`
- selected integration approach
- created realtime components
- event names
- group names
- authorization assumptions
- sample frontend connection/event contract if useful
