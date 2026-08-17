using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

public sealed class CargoDryStockRequestEntity : AizenEntityWithAudit
{
    public string RequestCode { get; private set; } = default!;
    public long ProviderProfileId { get; private set; }
    public string ProductCode { get; private set; } = default!;
    public long? ConsignmentAgreementId { get; private set; }
    public int RequestedQuantity { get; private set; }
    public CargoDryStockRequestStatus Status { get; private set; }
    public string? ProviderNote { get; private set; }
    public long? DecidedByUserId { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public string? DecisionNote { get; private set; }
    public string? ApprovedBatchCode { get; private set; }
    public int? AllocatedQuantity { get; private set; }

    public CargoDryStockRequestEntity() { }

    public static CargoDryStockRequestEntity Create(
        long providerProfileId, string productCode, int requestedQuantity,
        long? agreementId, string? note)
    {
        if (requestedQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedQuantity), "Must be > 0.");

        return new CargoDryStockRequestEntity
        {
            RequestCode = $"STR-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            ProviderProfileId = providerProfileId,
            ProductCode = productCode,
            RequestedQuantity = requestedQuantity,
            ConsignmentAgreementId = agreementId,
            ProviderNote = note,
            Status = CargoDryStockRequestStatus.Pending,
            IsActive = true
        };
    }

    public void Approve(long userId, string? note, string? batchCode, int? allocatedQty)
    {
        if (Status != CargoDryStockRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot approve a request in status {Status}.");
        Status = CargoDryStockRequestStatus.Approved;
        DecidedByUserId = userId;
        DecidedAtUtc = DateTimeOffset.UtcNow;
        DecisionNote = note;
        ApprovedBatchCode = batchCode;
        AllocatedQuantity = allocatedQty;
    }

    public void Reject(long userId, string reason)
    {
        if (Status != CargoDryStockRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot reject a request in status {Status}.");
        Status = CargoDryStockRequestStatus.Rejected;
        DecidedByUserId = userId;
        DecidedAtUtc = DateTimeOffset.UtcNow;
        DecisionNote = reason;
    }

    public void Cancel(string? reason)
    {
        if (Status != CargoDryStockRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot cancel a request in status {Status}.");
        Status = CargoDryStockRequestStatus.Cancelled;
        DecisionNote = reason;
    }

    public void MarkFulfilled()
    {
        if (Status != CargoDryStockRequestStatus.Approved)
            throw new InvalidOperationException($"Cannot fulfil a request in status {Status}.");
        Status = CargoDryStockRequestStatus.Fulfilled;
    }
}
