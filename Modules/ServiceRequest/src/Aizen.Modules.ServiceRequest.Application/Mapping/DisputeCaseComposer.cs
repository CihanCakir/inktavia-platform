using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.DisputeCase;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Entities.WorkLog;
using PayResp = Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

namespace Aizen.Modules.ServiceRequest.Application.Mapping;

/// <summary>
/// BE-S13a — pure composition (no logic) of the dispute case file from the dispute + SR graph + work-logs + the
/// cost-free Payment state. CONFIDENTIALITY (§20.9): this composer only ever reads cost-free fields — supplier cost /
/// dealer margin never enter the SR module, so the aggregate cannot leak them. Deterministic and side-effect-free so
/// it is unit-testable without a DB.
/// </summary>
public static class DisputeCaseComposer
{
    public static GetDisputeCaseDetailResponse Compose(
        ServiceRequestDisputeEntity dispute,
        ServiceRequestEntity sr,
        long? providerUserId,
        IReadOnlyList<ServiceRequestWorkLogEntity> workLogs,
        PayResp.GetDisputeCasePaymentStateRemoteCallResponse? paymentState)
    {
        return new GetDisputeCaseDetailResponse
        {
            Dispute        = MapDispute(dispute),
            ServiceRequest = MapServiceRequest(sr, providerUserId),
            Reason         = MapReason(dispute, sr),
            StatusTimeline = sr.StatusHistory
                .OrderBy(h => h.OccurredAt)
                .Select(MapHistory)
                .ToList(),
            Economics = MapEconomics(paymentState?.Economics),
            WorkLogs  = workLogs
                .OrderBy(w => w.LoggedAt)
                .Select(MapWorkLog)
                .ToList(),
            Completion = MapCompletion(sr),
            Messages   = sr.Messages
                .OrderBy(m => m.CreateDate)
                .Select(MapMessage)
                .ToList(),
            PaymentState = MapPaymentState(paymentState),
        };
    }

    private static ServiceRequestDisputeDto MapDispute(ServiceRequestDisputeEntity d) => new()
    {
        Id                      = d.Id,
        ServiceRequestId        = d.ServiceRequestId,
        OpenedByUserId          = d.OpenedByUserId,
        OpenedByActorType       = d.OpenedByActorType,
        Status                  = d.Status,
        Reason                  = d.Reason,
        Description             = d.Description,
        ResolutionNotes         = d.ResolutionNotes,
        ResolvedByAdminUserId   = d.ResolvedByAdminUserId,
        ResolvedAt              = d.ResolvedAt,
        OpenedAt                = d.OpenedAt,
        ResolutionOutcome       = d.ResolutionOutcome,
        ResolutionRefundAmount  = d.ResolutionRefundAmount,
        PaymentOutcomeAppliedAt = d.PaymentOutcomeAppliedAt,
    };

    private static DisputeCaseServiceRequestDto MapServiceRequest(ServiceRequestEntity sr, long? providerUserId) => new()
    {
        Id                   = sr.Id,
        RequestCode          = sr.RequestCode,
        Title                = sr.Title,
        Status               = sr.Status,
        ServiceCategoryCode  = sr.ServiceCategoryCode,
        OwnerUserId          = sr.OwnerUserId,
        ProviderUserId       = providerUserId,
        AssignedProviderName = sr.AssignedProviderName,
        PaymentTransactionId = sr.PaymentTransactionId,
        CreatedAt            = sr.CreateDate,
        CancelledAt          = sr.CancelledAt,
    };

    private static DisputeCaseReasonDto MapReason(ServiceRequestDisputeEntity d, ServiceRequestEntity sr) => new()
    {
        DisputeReason              = d.Reason,
        DisputeDescription         = d.Description,
        CancelReasonCode           = sr.CancelReasonCode,
        CompletionRejectReasonCode = sr.Completion?.RejectReasonCode,
    };

    private static ServiceRequestStatusHistoryDto MapHistory(ServiceRequestStatusHistoryEntity h) => new()
    {
        Id          = h.Id,
        FromStatus  = h.FromStatus,
        ToStatus    = h.ToStatus,
        Reason      = h.Reason,
        ActorUserId = h.ActorUserId,
        ActorType   = h.ActorType,
        OccurredAt  = h.OccurredAt,
    };

    private static DisputeCaseWorkLogDto MapWorkLog(ServiceRequestWorkLogEntity w) => new()
    {
        Id               = w.Id,
        ProviderUserId   = w.ProviderUserId,
        LogType          = w.LogType,
        Title            = w.Title,
        Description      = w.Description,
        AttachmentFileId = w.AttachmentFileId,
        LoggedAt         = w.LoggedAt,
    };

    private static DisputeCaseCompletionDto? MapCompletion(ServiceRequestEntity sr)
    {
        var c = sr.Completion;
        if (c is null) return null;
        return new DisputeCaseCompletionDto
        {
            Status           = c.Status,
            CompletionNotes  = c.CompletionNotes,
            EvidenceFileId   = c.EvidenceFileId,
            SubmittedAt      = c.SubmittedAt,
            ReviewedAt       = c.ReviewedAt,
            RejectReasonCode = c.RejectReasonCode,
            ClientRating     = c.ClientRating,
        };
    }

    private static DisputeCaseMessageDto MapMessage(ServiceRequestMessageEntity m) => new()
    {
        Id               = m.Id,
        SenderUserId     = m.SenderUserId,
        SenderType       = m.SenderType,
        MessageType      = m.MessageType,
        Content          = m.Content,
        AttachmentFileId = m.AttachmentFileId,
        CreatedAt        = m.CreateDate,
    };

    private static DisputeCaseEconomicsDto? MapEconomics(PayResp.DisputeEconomicsRemoteDto? e)
    {
        if (e is null) return null;
        return new DisputeCaseEconomicsDto
        {
            SnapshotId             = e.SnapshotId,
            SnapshotCode           = e.SnapshotCode,
            CurrencyCode           = e.CurrencyCode,
            ServiceAmount          = e.ServiceAmount,
            CommissionAmount       = e.CommissionAmount,
            ProviderNetAmount      = e.ProviderNetAmount,
            PlatformFeeGrossAmount = e.PlatformFeeGrossAmount,
            CustomerTotalAmount    = e.CustomerTotalAmount,
            Lines = e.Lines.Select(l => new DisputeCaseEconomicsLineDto
            {
                LineRef                = l.LineRef,
                ItemType               = l.ItemType,
                GrossBeforeDiscount    = l.GrossBeforeDiscount,
                CustomerDiscountAmount = l.CustomerDiscountAmount,
                CommissionAmount       = l.CommissionAmount,
                ProviderNetAmount      = l.ProviderNetAmount,
                LineVatAmount          = l.LineVatAmount,
                LineTotalAmount        = l.LineTotalAmount,
            }).ToList(),
        };
    }

    private static DisputeCasePaymentStateDto? MapPaymentState(PayResp.GetDisputeCasePaymentStateRemoteCallResponse? p)
    {
        if (p is null) return null;
        return new DisputeCasePaymentStateDto
        {
            HasTransaction      = p.HasTransaction,
            TransactionId       = p.TransactionId,
            TransactionCode     = p.TransactionCode,
            Status              = p.Status,
            CurrencyCode        = p.CurrencyCode,
            GrossAmount         = p.GrossAmount,
            TotalRefundedAmount = p.TotalRefundedAmount,
            RefundableAmount    = p.RefundableAmount,
            EscrowReleased      = p.EscrowReleased,
            CapturedAt          = p.CapturedAt,
            ReleasedAt          = p.ReleasedAt,
            RefundAllocations   = p.RefundAllocations.Select(a => new DisputeCaseRefundAllocationDto
            {
                RefundRecordId                  = a.RefundRecordId,
                RefundCode                      = a.RefundCode,
                RefundReason                    = a.RefundReason,
                RefundCause                     = a.RefundCause,
                Amount                          = a.Amount,
                RefundStatus                    = a.RefundStatus,
                ServiceRefundAmount             = a.ServiceRefundAmount,
                ProviderNetReversalAmount       = a.ProviderNetReversalAmount,
                CommissionRevenueReversalAmount = a.CommissionRevenueReversalAmount,
                PlatformFeeGrossRefundAmount    = a.PlatformFeeGrossRefundAmount,
                PlatformAdvancedRefundAmount    = a.PlatformAdvancedRefundAmount,
                ProcessedAt                     = a.ProcessedAt,
            }).ToList(),
            Chargeback = p.Chargeback is null ? null : new DisputeCaseChargebackDto
            {
                GatewayChargebackReference = p.Chargeback.GatewayChargebackReference,
                Amount                     = p.Chargeback.Amount,
                ProviderRecoveredAmount    = p.Chargeback.ProviderRecoveredAmount,
                RemainingNegativeBalance   = p.Chargeback.RemainingNegativeBalance,
                ReceivedAtUtc              = p.Chargeback.ReceivedAtUtc,
            },
        };
    }
}
