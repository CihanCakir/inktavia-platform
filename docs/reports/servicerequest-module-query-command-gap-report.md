# ServiceRequest Module Query & Command Gap Report

## Scope

Audit of existing ServiceRequest module CQRS queries and commands against the P0/P1/P2 target contract.

## Existing Queries

### Admin Queries
| Query | Handler | Status |
|-------|---------|--------|
| `GetAdminServiceRequestListQuery` | `GetAdminServiceRequestListQueryHandler` | ✅ Fixed (was broken — only OwnerUserId filter) |
| `GetAdminDisputeListQuery` | `GetAdminDisputeListQueryHandler` | ✅ Exists |

### ServiceRequest Queries
| Query | Handler | Status |
|-------|---------|--------|
| `GetServiceRequestDetailQuery` | `GetServiceRequestDetailQueryHandler` | ✅ Exists |
| `GetServiceRequestListQuery` | `GetServiceRequestListQueryHandler` | ✅ Exists |
| `GetServiceRequestMessagesQuery` | `GetServiceRequestMessagesQueryHandler` | ✅ Exists |
| `GetServiceRequestWorkLogsQuery` | `GetServiceRequestWorkLogsQueryHandler` | ✅ Exists |

## Existing Commands

### ServiceRequest Commands
| Command | Status | Exposed in BFF |
|---------|--------|----------------|
| `CreateServiceRequestCommand` | ✅ | Customer endpoint (not admin BFF) |
| `UpdateServiceRequestCommand` | ✅ | Customer endpoint |
| `PublishServiceRequestCommand` | ✅ | Customer endpoint |
| `CancelServiceRequestCommand` | ✅ | ✅ BFF exposed at `PATCH /admin-panel/service-requests/{id}/cancel` |
| `AddServiceRequestAttachmentCommand` | ✅ | Not exposed in admin BFF |

### Assignment Commands
| Command | Status | Exposed in BFF |
|---------|--------|----------------|
| `CreateServiceRequestAssignmentCommand` | ✅ | Not exposed in admin BFF (P2) |
| `AcceptServiceRequestAssignmentCommand` | ✅ | Not exposed in admin BFF |
| `RejectServiceRequestAssignmentCommand` | ✅ | Not exposed in admin BFF |
| `StartServiceRequestAssignmentCommand` | ✅ | Not exposed in admin BFF |

### Offer Commands
| Command | Status | Exposed in BFF |
|---------|--------|----------------|
| `CreateServiceRequestOfferCommand` | ✅ | Provider endpoint (not admin) |
| `UpdateServiceRequestOfferCommand` | ✅ | Provider endpoint |
| `AcceptServiceRequestOfferCommand` | ✅ | Customer endpoint |
| `RejectServiceRequestOfferCommand` | ✅ | Customer endpoint |
| `WithdrawServiceRequestOfferCommand` | ✅ | Provider endpoint |

### Completion Commands
| Command | Status | Exposed in BFF |
|---------|--------|----------------|
| `SubmitServiceRequestCompletionCommand` | ✅ | Provider endpoint |
| `ApproveServiceRequestCompletionCommand` | ✅ | ✅ BFF exposed at `PATCH /admin-panel/service-requests/{id}/completion/approve` |
| `RejectServiceRequestCompletionCommand` | ✅ | ✅ BFF exposed at `PATCH /admin-panel/service-requests/{id}/completion/reject` |

### Dispute Commands
| Command | Status | Exposed in BFF |
|---------|--------|----------------|
| `OpenServiceRequestDisputeCommand` | ✅ | Customer/Provider endpoint (not admin BFF) |
| `ChangeServiceRequestDisputeStatusCommand` | ✅ | ✅ BFF exposed at `PATCH /admin-panel/service-requests/{id}/disputes/{disputeId}/status` |
| `ResolveServiceRequestDisputeCommand` | ✅ | ✅ BFF exposed at `PATCH /admin-panel/service-requests/{id}/disputes/{disputeId}/resolve` |

### WorkLog Commands
| Command | Status | Exposed in BFF |
|---------|--------|----------------|
| `AddServiceRequestWorkLogCommand` | ✅ | Provider endpoint (not admin) |

## P1 Read Endpoint Gaps (not implemented, documented)

| Endpoint | Gap Reason |
|----------|-----------|
| `GET /admin-panel/service-requests/{id}/offers` | No BFF query wrapping — SR module has `GetServiceRequestDetailQuery` which includes offers |
| `GET /admin-panel/service-requests/{id}/assignments` | No BFF query wrapping — SR detail includes assignment |
| `GET /admin-panel/service-requests/{id}/worklogs` | `GetServiceRequestWorkLogsQuery` exists in SR module but not BFF-exposed |
| `GET /admin-panel/service-requests/{id}/messages` | `GetServiceRequestMessagesQuery` exists in SR module but not BFF-exposed |
| `GET /admin-panel/service-requests/{id}/completion-dispute` | SR detail includes completion/dispute — no dedicated BFF endpoint |

## P2 Admin Command Gaps

| Command | Domain Status | BFF Status | Notes |
|---------|--------------|------------|-------|
| `POST /admin-panel/service-requests/{id}/assign` | `CreateServiceRequestAssignmentCommand` exists | ❌ Not exposed | P2 — expose in future sprint |
| `POST /admin-panel/service-requests/{id}/status` | `ChangeStatus` exists on entity | ❌ No admin status command | Would require new admin command |
