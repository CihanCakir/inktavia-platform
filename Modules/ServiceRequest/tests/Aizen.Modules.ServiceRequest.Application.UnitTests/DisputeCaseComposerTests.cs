using System.Collections;
using System.Reflection;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Entities.WorkLog;
using FluentAssertions;
using PayResp = Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE-S13a — the dispute case aggregate composes the whole file (SR + economics + work-logs + evidence + messages +
/// refund state + reason + timeline) and, critically, surfaces NO supplier cost / dealer margin (§20.9). The no-cost
/// guard walks the entire response object graph by reflection.
/// </summary>
public sealed class DisputeCaseComposerTests
{
    private static ServiceRequestEntity BuildServiceRequest()
    {
        var sr = ServiceRequestEntity.Create(
            requestCode: "SR-TEST-1", ownerUserId: 7, vesselId: 3,
            serviceCategoryCode: "electrical", serviceTypeCode: null,
            title: "Fix wiring", description: "desc", priority: ServiceRequestPriority.Normal,
            requestedStartDate: null, requestedEndDate: null,
            locationCountryCode: "TR", locationCityCode: "35", locationMarinaName: null,
            locationLatitude: null, locationLongitude: null, ownerNotes: null, expiresAt: null);

        sr.Cancel(cancelledByUserId: 7, reason: "found another", ServiceRequestCancelReason.FoundAnotherProvider);

        sr.AddStatusHistory(ServiceRequestStatusHistoryEntity.Create(
            sr.Id, ServiceRequestStatus.InProgress, ServiceRequestStatus.DisputeOpened,
            "Dispute opened", 7, ServiceRequestActorType.Owner));

        sr.AddMessage(ServiceRequestMessageEntity.Create(
            sr.Id, senderUserId: 88, ServiceRequestMessageSenderType.Provider,
            ServiceRequestMessageType.Text, "Work is done", attachmentFileId: null));

        var completion = ServiceRequestCompletionEntity.Create(
            sr.Id, serviceRequestAssignmentId: 5, providerUserId: 88,
            completionNotes: "all fixed", evidenceFileId: Guid.NewGuid());
        completion.RejectByOwner(reviewerUserId: 7, reviewNotes: "not fixed", CompletionRejectReason.WorkIncomplete);
        sr.SetCompletion(completion);

        return sr;
    }

    private static PayResp.GetDisputeCasePaymentStateRemoteCallResponse BuildPaymentState() => new()
    {
        HasTransaction = true, TransactionId = 500, TransactionCode = "TXN-1", Status = "Captured",
        CurrencyCode = "TRY", GrossAmount = 1000m, TotalRefundedAmount = 0m, RefundableAmount = 1000m,
        EscrowReleased = false, CapturedAt = DateTime.UtcNow,
        Economics = new PayResp.DisputeEconomicsRemoteDto
        {
            SnapshotId = 900, SnapshotCode = "PES-1", CurrencyCode = "TRY",
            ServiceAmount = 800m, CommissionAmount = 120m, ProviderNetAmount = 680m,
            PlatformFeeGrossAmount = 200m, CustomerTotalAmount = 1000m,
            Lines = new()
            {
                new PayResp.DisputeEconomicsLineRemoteDto
                {
                    LineRef = "L1", ItemType = "Labor", GrossBeforeDiscount = 800m,
                    CustomerDiscountAmount = 0m, CommissionAmount = 120m, ProviderNetAmount = 680m,
                    LineVatAmount = 0m, LineTotalAmount = 800m,
                },
            },
        },
        RefundAllocations = new()
        {
            new PayResp.DisputeRefundAllocationRemoteDto
            {
                RefundRecordId = 1, RefundCode = "REF-1", RefundReason = "DisputeResolvedForPayer",
                RefundCause = "DisputeCustomerFavoured", Amount = 300m, RefundStatus = "Processed",
                ServiceRefundAmount = 240m, ProviderNetReversalAmount = 204m,
                CommissionRevenueReversalAmount = 36m, PlatformFeeGrossRefundAmount = 60m,
                PlatformAdvancedRefundAmount = 0m, ProcessedAt = DateTime.UtcNow,
            },
        },
        Chargeback = new PayResp.DisputeChargebackRemoteDto
        {
            GatewayChargebackReference = "CB-1", Amount = 100m,
            ProviderRecoveredAmount = 100m, RemainingNegativeBalance = 0m, ReceivedAtUtc = DateTime.UtcNow,
        },
    };

    private static (ServiceRequestDisputeEntity dispute, ServiceRequestEntity sr, IReadOnlyList<ServiceRequestWorkLogEntity> logs) BuildInputs()
    {
        var sr = BuildServiceRequest();
        var dispute = ServiceRequestDisputeEntity.Create(
            sr.Id, openedByUserId: 7, ServiceRequestActorType.Owner,
            ServiceRequestDisputeReason.QualityIssue, "not as agreed");
        dispute.Resolve(adminUserId: 99, resolutionNotes: "partial refund");
        dispute.MarkPaymentOutcomeApplied(DisputeResolutionOutcome.FavorPayerPartialRefund, 300m);

        var logs = new List<ServiceRequestWorkLogEntity>
        {
            ServiceRequestWorkLogEntity.Create(
                sr.Id, serviceRequestAssignmentId: 5, providerUserId: 88,
                ServiceRequestWorkLogType.WorkStarted, "Started", "began work", null, null, null),
        };
        return (dispute, sr, logs);
    }

    [Fact] // test (1) — composes every part of the case file
    public void Composes_all_parts_of_the_case_file()
    {
        var (dispute, sr, logs) = BuildInputs();

        var response = DisputeCaseComposer.Compose(dispute, sr, providerUserId: 88, logs, BuildPaymentState());

        response.Dispute.Id.Should().Be(dispute.Id);
        response.ServiceRequest.RequestCode.Should().Be("SR-TEST-1");
        response.ServiceRequest.ProviderUserId.Should().Be(88);
        response.StatusTimeline.Should().ContainSingle();
        response.Reason.DisputeReason.Should().Be(ServiceRequestDisputeReason.QualityIssue);
        response.Reason.CancelReasonCode.Should().Be(ServiceRequestCancelReason.FoundAnotherProvider);
        response.Reason.CompletionRejectReasonCode.Should().Be(CompletionRejectReason.WorkIncomplete);
        response.Economics.Should().NotBeNull();
        response.Economics!.Lines.Should().ContainSingle();
        response.WorkLogs.Should().ContainSingle();
        response.Completion.Should().NotBeNull();
        response.Completion!.EvidenceFileId.Should().NotBeNull();
        response.Messages.Should().ContainSingle();
        response.PaymentState.Should().NotBeNull();
        response.PaymentState!.RefundAllocations.Should().ContainSingle();
        response.PaymentState.Chargeback.Should().NotBeNull();
        response.PaymentState.RefundableAmount.Should().Be(1000m);
    }

    [Fact] // BE_WC3b — the transcript now sources from the Messaging store (not sr.Messages); role/type + fileId mapped
    public void Transcript_sources_from_Messaging_and_maps_role_type_and_fileId()
    {
        var (dispute, sr, logs) = BuildInputs();   // sr has one sr.Messages row ("Work is done") that must NOT be used
        var imageFileId = Guid.NewGuid();
        var t0 = DateTimeOffset.UtcNow.AddMinutes(-10);

        // Messaging enum values: SenderRole Owner=1/Provider=2/Admin=3/System=4; MessageType Text=1/SystemNotification=2/
        // StatusChange=3/MediaAttachment=5/Location=6.
        var transcript = new List<SrTranscriptMessageDto>
        {
            new() { MessageId = 501, SenderUserId = 7,  SenderRole = 1, MessageType = 1, Content = "owner hello", SentAt = t0 },
            new() { MessageId = 502, SenderUserId = 88, SenderRole = 2, MessageType = 5, Content = "",
                    AttachmentFileStorageId = imageFileId.ToString(), SentAt = t0.AddMinutes(1) },
            new() { MessageId = 503, SenderUserId = 0,  SenderRole = 4, MessageType = 2, Content = "system note", SentAt = t0.AddMinutes(2) },
            new() { MessageId = 504, SenderUserId = 7,  SenderRole = 1, MessageType = 6, Content = "{\"lat\":1,\"lng\":2}",
                    LocationLat = 1m, LocationLng = 2m, LocationLabel = "Marina", SentAt = t0.AddMinutes(3) },
        };

        var response = DisputeCaseComposer.Compose(dispute, sr, providerUserId: 88, logs, BuildPaymentState(), transcript);

        response.Messages.Should().HaveCount(4);
        response.Messages.Select(m => m.Content).Should().NotContain("Work is done"); // NOT from sr.Messages

        var owner = response.Messages.Single(m => m.Id == 501);
        owner.SenderType.Should().Be(ServiceRequestMessageSenderType.Owner);
        owner.MessageType.Should().Be(ServiceRequestMessageType.Text);

        var image = response.Messages.Single(m => m.Id == 502);
        image.SenderType.Should().Be(ServiceRequestMessageSenderType.Provider);
        image.MessageType.Should().Be(ServiceRequestMessageType.Image);   // MediaAttachment(5) → Image
        image.AttachmentFileId.Should().Be(imageFileId);                  // string → Guid

        response.Messages.Single(m => m.Id == 503).SenderType.Should().Be(ServiceRequestMessageSenderType.System);
        response.Messages.Single(m => m.Id == 503).MessageType.Should().Be(ServiceRequestMessageType.SystemNotification);
        response.Messages.Single(m => m.Id == 504).MessageType.Should().Be(ServiceRequestMessageType.Location);
    }

    [Fact] // BE_WC3b — a null transcript (Messaging read failed) falls back to sr.Messages so the case never loses it
    public void Null_transcript_falls_back_to_sr_messages()
    {
        var (dispute, sr, logs) = BuildInputs();

        var response = DisputeCaseComposer.Compose(dispute, sr, providerUserId: 88, logs, BuildPaymentState(), transcript: null);

        response.Messages.Should().ContainSingle();
        response.Messages[0].Content.Should().Be("Work is done"); // the sr.Messages fallback row
    }

    [Fact] // test (1) — CONFIDENTIALITY: no supplier cost / dealer margin anywhere in the graph
    public void Case_file_surfaces_no_cost_or_margin_fields()
    {
        var (dispute, sr, logs) = BuildInputs();

        var response = DisputeCaseComposer.Compose(dispute, sr, providerUserId: 88, logs, BuildPaymentState());

        var offendingProperties = new List<string>();
        ScanForCostFields(response, "response", new HashSet<object>(ReferenceEqualityComparer.Instance), offendingProperties);
        offendingProperties.Should().BeEmpty(
            "the dispute case aggregate must never surface supplier cost / dealer margin (§20.9)");
    }

    private static readonly string[] ForbiddenTokens =
        { "supplierlistprice", "dealermargin", "dealercost", "suppliercost" };

    // Walks the response object graph; any property whose NAME matches a forbidden cost/margin token is a leak.
    private static void ScanForCostFields(object? node, string path, HashSet<object> visited, List<string> offenders)
    {
        if (node is null) return;
        var type = node.GetType();
        if (type.IsPrimitive || node is string or decimal or DateTime or Guid or Enum) return;
        if (!visited.Add(node)) return;

        if (node is IEnumerable enumerable)
        {
            var i = 0;
            foreach (var item in enumerable)
                ScanForCostFields(item, $"{path}[{i++}]", visited, offenders);
            return;
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var name = prop.Name.ToLowerInvariant();
            if (ForbiddenTokens.Any(t => name.Contains(t)))
                offenders.Add($"{path}.{prop.Name}");

            object? value;
            try { value = prop.GetValue(node); }
            catch { continue; }
            ScanForCostFields(value, $"{path}.{prop.Name}", visited, offenders);
        }
    }
}
