# Inktavia Marine OS - ServiceRequest Advanced MVP Realtime CQRS

Use this prompt with GitHub Copilot Agent inside the repository.

All generated implementation must be in English and must follow the existing Aizen/Inktavia architecture.

## Step 06 - Application commands and handlers

Implement CQRS commands, handlers, validators, mappings, and command responses.

## General rules

- Use typed responses only.
- Do not return `object`.
- Use existing `Result`, `MetropolApiResponse`, `AizenResponse`, or equivalent project response wrapper.
- Use `IAizenInfoAccessor` or equivalent to get current user/client/device context.
- Validate access before state changes.
- Invalidate cache after successful writes.
- Publish realtime events after successful committed changes.
- Add status history for every state transition.
- Use FileStorage validation for attachments by `FileId` if FileStorage service contracts are available.
- Use Vessel access validation before owner-sensitive operations.
- Use ProviderOperations validation for provider-sensitive operations.

## Request commands

Implement:

```text
CreateServiceRequestCommand
UpdateServiceRequestCommand
CancelServiceRequestCommand
ArchiveServiceRequestCommand
ChangeServiceRequestStatusCommand
AddServiceRequestAttachmentCommand
RemoveServiceRequestAttachmentCommand
```

Expected returns:

```text
ServiceRequestDetailDto
ServiceRequestDto
bool or typed operation response according to project standard
```

## Offer commands

Implement:

```text
CreateServiceRequestOfferCommand
UpdateServiceRequestOfferCommand
WithdrawServiceRequestOfferCommand
AcceptServiceRequestOfferCommand
RejectServiceRequestOfferCommand
```

Rules:

- Provider can create offers only for visible/eligible requests.
- Offer must contain at least one valid offer item unless existing business allows summary-only offers.
- Money amounts must be non-negative except discount item types if allowed.
- Accepted offer must be unique for the request.
- Accepting an offer should update request status and publish realtime event.

## Assignment commands

Implement:

```text
CreateServiceRequestAssignmentCommand
UpdateServiceRequestAssignmentCommand
AcceptServiceRequestAssignmentCommand
RejectServiceRequestAssignmentCommand
StartServiceRequestAssignmentCommand
CompleteServiceRequestAssignmentCommand
CancelServiceRequestAssignmentCommand
```

Rules:

- Assignment normally requires an accepted offer.
- Admin may override if the business rule allows it. If override is not supported, reject it.
- Starting assignment moves request to `InProgress`.
- Completing assignment can prepare completion submission but should not close the request unless completion is approved.

## Message commands

Implement:

```text
SendServiceRequestMessageCommand
MarkServiceRequestMessageAsReadCommand
```

Rules:

- Message visibility must be scoped to request participants and admin.
- Publish `MessageSent` realtime event.

## WorkLog commands

Implement:

```text
AddServiceRequestWorkLogCommand
UpdateServiceRequestWorkLogCommand
RemoveServiceRequestWorkLogCommand
```

Rules:

- Provider/admin can add work logs after assignment/in-progress state.
- Owner normally cannot add operational work logs unless existing business explicitly supports owner notes.
- Attachments must be FileStorage references.
- Publish `WorkLogAdded`, `WorkStarted`, `WorkPaused`, or `WorkResumed` as appropriate.

## Completion commands

Implement:

```text
SubmitServiceRequestCompletionCommand
ApproveServiceRequestCompletionCommand
RejectServiceRequestCompletionCommand
```

Rules:

- Provider/admin submits completion.
- Owner approves or rejects completion.
- Approval moves request to `Completed` or `Closed` according to status design.
- Rejection may move request to `DisputeOpened` or `WaitingForProviderResponse` based on request payload.

## Dispute commands

Implement:

```text
OpenServiceRequestDisputeCommand
AddServiceRequestDisputeMessageCommand
AddServiceRequestDisputeAttachmentCommand
ChangeServiceRequestDisputeStatusCommand
ResolveServiceRequestDisputeCommand
CancelServiceRequestDisputeCommand
```

Rules:

- Owner, provider, or admin can open dispute based on access and status.
- Admin can resolve dispute.
- Dispute status changes must be tracked and published realtime.

## Validators

Create validators for all commands/request models using existing validation pattern.

## Realtime publishing

Every successful command that changes state or adds visible information must call the ServiceRequest realtime publisher service.

## Output required

Create/update:

```text
ai/service-request-advanced-mvp-realtime-cqrs/reports/06_APPLICATION_COMMANDS_REPORT.md
```
