# Inktavia Marine OS - ServiceRequest Advanced MVP Realtime CQRS

Use this prompt with GitHub Copilot Agent inside the repository.

All generated implementation must be in English and must follow the existing Aizen/Inktavia architecture.

## Step 03 - Create Abstraction contracts, enums, DTOs, requests, responses

Implement the Abstraction layer for ServiceRequest.

## Enums

Create enums under the existing enum convention. Include at minimum:

```text
ServiceRequestStatus
ServiceRequestPriority
ServiceRequestItemType
ServiceRequestAttachmentType
ServiceRequestMessageType
ServiceRequestMessageSenderType
ServiceRequestOfferStatus
ServiceRequestOfferItemType
ServiceRequestAssignmentStatus
ServiceRequestWorkLogType
ServiceRequestCompletionStatus
ServiceRequestDisputeStatus
ServiceRequestDisputeReason
ServiceRequestRealtimeEventType
ServiceRequestActorType
ServiceRequestVisibilityScope
```

Suggested `ServiceRequestStatus` values:

```text
Draft = 1
Open = 10
WaitingForOffer = 11
OfferReceived = 12
OfferAccepted = 13
OfferRejected = 14
WaitingForAssignment = 20
Assigned = 21
Scheduled = 22
InProgress = 30
WaitingForOwnerApproval = 31
WaitingForMaterial = 32
Paused = 33
CompletionSubmitted = 40
Completed = 41
DisputeOpened = 50
UnderDisputeReview = 51
DisputeResolved = 52
Cancelled = 90
Expired = 91
Closed = 99
```

Suggested `ServiceRequestOfferItemType` values:

```text
Service = 1
Product = 2
Installation = 3
Delivery = 4
Labor = 5
Inspection = 6
EmergencyFee = 7
Discount = 8
Other = 99
```

Suggested `ServiceRequestWorkLogType` values:

```text
GeneralNote = 1
ArrivedAtVessel = 2
InspectionStarted = 3
InspectionCompleted = 4
WorkStarted = 5
MaterialRequired = 6
AdditionalIssueFound = 7
WaitingForOwnerApproval = 8
WorkPaused = 9
WorkResumed = 10
WorkCompleted = 11
```

## DTOs

Create typed DTOs for all output. Include at minimum:

```text
ServiceRequestDto
ServiceRequestSummaryDto
ServiceRequestDetailDto
ServiceRequestItemDto
ServiceRequestOfferDto
ServiceRequestOfferItemDto
ServiceRequestStatusHistoryDto
ServiceRequestAttachmentDto
ServiceRequestMessageDto
ServiceRequestAssignmentDto
ServiceRequestWorkLogDto
ServiceRequestCompletionDto
ServiceRequestDisputeDto
ServiceRequestTimelineDto
ServiceRequestDashboardDto
ServiceRequestRealtimeEventDto
ServiceRequestLocationSnapshotDto
ServiceRequestPricingSummaryDto
```

## Request models

Create controller request models. Include at minimum:

```text
CreateServiceRequestRequest
UpdateServiceRequestRequest
CancelServiceRequestRequest
AddServiceRequestAttachmentRequest
RemoveServiceRequestAttachmentRequest
CreateServiceRequestOfferRequest
UpdateServiceRequestOfferRequest
AcceptServiceRequestOfferRequest
RejectServiceRequestOfferRequest
CreateServiceRequestAssignmentRequest
UpdateServiceRequestAssignmentRequest
AcceptServiceRequestAssignmentRequest
RejectServiceRequestAssignmentRequest
StartServiceRequestAssignmentRequest
CompleteServiceRequestAssignmentRequest
SendServiceRequestMessageRequest
MarkServiceRequestMessageAsReadRequest
AddServiceRequestWorkLogRequest
UpdateServiceRequestWorkLogRequest
SubmitServiceRequestCompletionRequest
ApproveServiceRequestCompletionRequest
RejectServiceRequestCompletionRequest
OpenServiceRequestDisputeRequest
AddServiceRequestDisputeMessageRequest
AddServiceRequestDisputeAttachmentRequest
ChangeServiceRequestDisputeStatusRequest
ResolveServiceRequestDisputeRequest
```

## Filter request models

Create query/filter request models:

```text
ServiceRequestListFilterRequest
ProviderAvailableServiceRequestFilterRequest
ProviderAssignedServiceRequestFilterRequest
AdminServiceRequestFilterRequest
AdminDisputeFilterRequest
ServiceRequestMessageListRequest
ServiceRequestTimelineRequest
```

Use project-standard pagination request/response types if available.

## Realtime DTO

Create a typed realtime event DTO. Avoid using `object` in handler returns. If payload must be dynamic, use a safe serialized payload or typed generic model consistent with `Aizen.Core.Realtime`.

Example logical shape:

```csharp
public sealed class ServiceRequestRealtimeEventDto
{
    public Guid ServiceRequestId { get; set; }
    public ServiceRequestRealtimeEventType EventType { get; set; }
    public string? PayloadType { get; set; }
    public string? PayloadJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Guid? ActorUserId { get; set; }
}
```

Adapt this to the existing realtime framework.

## Required standards

- Add `DocumentationInfo` to public types.
- Use nullable reference types correctly.
- Do not put EF Core entities in Abstraction.
- Do not return domain entities from API responses.
- Keep DTOs stable and frontend-friendly.

## Output required

Create/update:

```text
ai/service-request-advanced-mvp-realtime-cqrs/reports/03_ABSTRACTION_REPORT.md
```
