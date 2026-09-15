using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Entities;

namespace Aizen.Modules.CargoDry.Application.Common;

/// <summary>Single source of truth for CargoDryStockRequestEntity → DTO projection (incl. the Wave 4A lifecycle fields).</summary>
public static class CargoDryStockRequestMapper
{
    public static CargoDryStockRequestDto ToDto(this CargoDryStockRequestEntity e) => new()
    {
        Id                     = e.Id,
        RequestCode            = e.RequestCode,
        ProviderProfileId      = e.ProviderProfileId,
        ProductCode            = e.ProductCode,
        RequestedQuantity      = e.RequestedQuantity,
        Status                 = (int)e.Status,
        StatusName             = e.Status.ToString(),
        ProviderNote           = e.ProviderNote,
        ConsignmentAgreementId = e.ConsignmentAgreementId,
        DecidedAtUtc           = e.DecidedAtUtc,
        DecisionNote           = e.DecisionNote,
        ApprovedBatchCode      = e.ApprovedBatchCode,
        AllocatedQuantity      = e.AllocatedQuantity,
        TrackingCode           = e.TrackingCode,
        ShippedAtUtc           = e.ShippedAtUtc,
        AutoReceiveDeadlineUtc = e.AutoReceiveDeadlineUtc,
        ReceivedAtUtc          = e.ReceivedAtUtc,
        CreatedAtUtc           = e.CreateDate.HasValue ? new DateTimeOffset(e.CreateDate.Value, TimeSpan.Zero) : DateTimeOffset.UtcNow,
    };
}
