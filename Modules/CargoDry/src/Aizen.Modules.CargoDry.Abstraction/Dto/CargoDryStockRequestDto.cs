namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryStockRequestDto
{
    public long Id { get; init; }
    public string RequestCode { get; init; } = default!;
    public long ProviderProfileId { get; init; }
    public string ProductCode { get; init; } = default!;
    public int RequestedQuantity { get; init; }
    public int Status { get; init; }
    public string StatusName { get; init; } = default!;
    public string? ProviderNote { get; init; }
    public long? ConsignmentAgreementId { get; init; }
    public DateTimeOffset? DecidedAtUtc { get; init; }
    public string? DecisionNote { get; init; }
    public string? ApprovedBatchCode { get; init; }
    public int? AllocatedQuantity { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed class CargoDryStockRequestPagedResultDto
{
    public List<CargoDryStockRequestDto> Items { get; init; } = new();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
