# ServiceRequest Module — Postman Validation Report

## Endpoint Coverage

| Controller | Endpoint | Method | Route | Covered | Sample | Tests | Notes |
|---|---|---|---|---|---|---|---|
| ServiceRequestController | Create Service Request | POST | /service-requests | ✅ Yes | ✅ Yes | ✅ Yes | Extracts serviceRequestId |
| ServiceRequestController | Update Service Request | PUT | /service-requests/{id} | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestController | Get Service Request | GET | /service-requests/{id} | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestController | Get My Service Requests | GET | /service-requests/my | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestController | Cancel Service Request | PATCH | /service-requests/{id}/cancel | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestController | Publish Service Request | PATCH | /service-requests/{id}/publish | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestController | Add Attachment | POST | /service-requests/{id}/attachments | ✅ Yes | ✅ Yes | ✅ Yes | Requires fileId |
| ServiceRequestOfferController | Create Offer | POST | /service-requests/{id}/offers | ✅ Yes | ✅ Yes | ✅ Yes | Extracts offerId |
| ServiceRequestOfferController | Update Offer | PUT | /service-requests/{id}/offers/{offerId} | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestOfferController | Accept Offer | PATCH | /service-requests/{id}/offers/{offerId}/accept | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestOfferController | Reject Offer | PATCH | /service-requests/{id}/offers/{offerId}/reject | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestOfferController | Withdraw Offer | PATCH | /service-requests/{id}/offers/{offerId}/withdraw | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestAssignmentController | Create Assignment | POST | /service-requests/{id}/assignment | ✅ Yes | ✅ Yes | ✅ Yes | Extracts assignmentId |
| ServiceRequestAssignmentController | Accept Assignment | PATCH | /service-requests/{id}/assignment/{assignmentId}/accept | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestAssignmentController | Reject Assignment | PATCH | /service-requests/{id}/assignment/{assignmentId}/reject | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestAssignmentController | Start Assignment | PATCH | /service-requests/{id}/assignment/{assignmentId}/start | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestMessageController | Send Message | POST | /service-requests/{id}/messages | ✅ Yes | ✅ Yes | ✅ Yes | Extracts messageId |
| ServiceRequestMessageController | Get Messages | GET | /service-requests/{id}/messages | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestMessageController | Mark Read | PATCH | /service-requests/{id}/messages/mark-read | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestWorkLogController | Add Work Log | POST | /service-requests/{id}/work-logs/assignment/{assignmentId} | ✅ Yes | ✅ Yes | ✅ Yes | Extracts workLogId |
| ServiceRequestWorkLogController | Get Work Logs | GET | /service-requests/{id}/work-logs/assignment/{assignmentId} | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestCompletionController | Submit Completion | POST | /service-requests/{id}/completion/{assignmentId} | ✅ Yes | ✅ Yes | ✅ Yes | Extracts completionId |
| ServiceRequestCompletionController | Approve Completion | PATCH | /service-requests/{id}/completion/approve | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestCompletionController | Reject Completion | PATCH | /service-requests/{id}/completion/reject | ✅ Yes | ✅ Yes | ✅ Yes | |
| ServiceRequestDisputeController | Open Dispute | POST | /service-requests/{id}/dispute | ✅ Yes | ✅ Yes | ✅ Yes | Extracts disputeId |
| ServiceRequestDisputeController | Change Dispute Status | PATCH | /service-requests/{id}/dispute/{disputeId}/status | ✅ Yes | ✅ Yes | ✅ Yes | Admin only |
| ServiceRequestDisputeController | Resolve Dispute | PATCH | /service-requests/{id}/dispute/{disputeId}/resolve | ✅ Yes | ✅ Yes | ✅ Yes | Admin only |
| AdminServiceRequestController | Admin List Service Requests | GET | /admin/service-requests | ✅ Yes | ✅ Yes | ✅ Yes | Admin only |
| AdminServiceRequestController | Admin List Disputes | GET | /admin/service-requests/disputes | ✅ Yes | ✅ Yes | ✅ Yes | Admin only |
| AdminServiceRequestController | Admin Get Service Request | GET | /admin/service-requests/{id} | ✅ Yes | ✅ Yes | ✅ Yes | Admin only |

## Coverage Statistics

| Category | Count | Covered | Coverage % |
|---|---|---|---|
| Total Endpoints | 30 | 30 | 100% |
| With Sample Request | 30 | 30 | 100% |
| With Test Scripts | 30 | 30 | 100% |
| ID Extraction Scripts | 7 | 7 | 100% |

## Notes
- Multi-actor scenario requires separate token management (owner vs provider)
- Status transitions must be followed in sequence (cannot Accept an Assignment that is not Assigned)
- Dispute endpoints are Admin-only for status change and resolve
- SignalR real-time events are tested in ServiceRequest.realtime-testing-guide.md
