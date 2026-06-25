# ServiceRequest Mock Data Specification

## Objective

Generate realistic ServiceRequest mock data linked to Identity UserIds and VesselIds.

## Required audit

Before generating mock JSON, inspect:

```text
Aizen.Modules.ServiceRequest.Domain
Aizen.Modules.ServiceRequest.Repository
Aizen.Modules.ServiceRequest.Application
Aizen.Modules.ServiceRequest.Abstraction
ServiceRequestDbContext
ServiceRequest entities
Offer entities
Assignment entities
Status history entities
Attachment entities
Message entities
WorkLog entities
Completion entities
Dispute entities
Enum definitions
Validation rules
Existing seeders
Realtime/outbox/message patterns if relevant
```

Do not invent lifecycle fields. Use actual domain status enum values.

## Relationship requirement

Use stable mock IDs from:

```text
Identity mock UserIds
Vessel mock VesselIds
```

Examples only; adapt to real entity fields:

```text
RequesterUserId -> boat owner Identity UserId
VesselId -> Vessel mock ID
AssignedProviderUserId -> provider/operator Identity UserId if supported
CreatedByUserId -> owner/admin Identity UserId
```

## Required lifecycle coverage

Create service requests across the lifecycle statuses supported by real entities.

Desired coverage:

```text
Draft/New
Submitted/Open
OfferReceived
Assigned
InProgress
WaitingCompletionApproval
Completed
Disputed
Cancelled
```

Use actual enum/status names from the codebase.

## Suggested scenarios

Create data for Admin Panel screens:

```text
Winterization preparation for Blue Octopus
Engine inspection for Marmara Pearl
Electrical panel repair for Golden Tide
Hull cleaning and antifouling for Bodrum Star
Cabin moisture and odor control for CargoDry Demo Boat
Emergency bilge pump check for Kalamis Runner
Disputed completion approval for Aegean Wind
Completed routine service for Gocek Breeze
```

## Detail-page data

Where entities exist, include:

```text
ServiceRequestItems
Offers and OfferItems
Assignments
WorkLogs
StatusHistory
Attachments
Messages
Completion evidence
Disputes
Dispute messages/notes
```

If FileStorage is referenced by attachment FileId, use deterministic placeholder FileIds and document them. Do not seed FileStorage DB from ServiceRequest unless FileStorage is explicitly in scope.
