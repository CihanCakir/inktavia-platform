using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>Full detail DTO for a single consignment agreement.</summary>
public sealed class CargoDryConsignmentAgreementDto
{
    public long   Id                { get; init; }
    public string AgreementCode     { get; init; } = default!;
    public long   ProviderProfileId { get; init; }
    public string ProductCode       { get; init; } = default!;

    // ── Commercial terms ───────────────────────────────────────────────────────
    public decimal ConsignmentRate          { get; init; }
    public decimal MinimumSettlementAmount  { get; init; }
    public string  CurrencyCode             { get; init; } = default!;
    public int     MaxKitCount              { get; init; }
    public int     AllocatedKitCount        { get; init; }
    public int     RemainingKitCount        { get; init; }

    // ── Status & lifecycle ─────────────────────────────────────────────────────
    public ConsignmentAgreementStatus Status       { get; init; }
    public string                     StatusName   { get; init; } = default!;
    public DateTime                   StartDateUtc { get; init; }
    public DateTime?                  EndDateUtc   { get; init; }

    // ── Optional metadata ──────────────────────────────────────────────────────
    public string? TermsDocumentRef   { get; init; }
    public string? Notes              { get; init; }

    // ── Status timestamps ──────────────────────────────────────────────────────
    public DateTime? ActivatedAtUtc   { get; init; }
    public DateTime? SuspendedAtUtc   { get; init; }
    public DateTime? TerminatedAtUtc  { get; init; }
    public string?   SuspendReason    { get; init; }
    public string?   TerminationReason { get; init; }

    // ── Audit ──────────────────────────────────────────────────────────────────
    public string? CreatedAt { get; init; }
    public string? UpdatedAt { get; init; }
}

/// <summary>Lightweight list-item DTO for paged agreement lists.</summary>
public sealed class CargoDryConsignmentAgreementListItemDto
{
    public long                       Id                { get; init; }
    public string                     AgreementCode     { get; init; } = default!;
    public long                       ProviderProfileId { get; init; }
    public string                     ProductCode       { get; init; } = default!;
    public decimal                    ConsignmentRate   { get; init; }
    public string                     CurrencyCode      { get; init; } = default!;
    public int                        MaxKitCount       { get; init; }
    public int                        AllocatedKitCount { get; init; }
    public int                        RemainingKitCount { get; init; }
    public ConsignmentAgreementStatus Status            { get; init; }
    public string                     StatusName        { get; init; } = default!;
    public DateTime                   StartDateUtc      { get; init; }
    public DateTime?                  EndDateUtc        { get; init; }
    public string?                    CreatedAt         { get; init; }
}

/// <summary>Paged result wrapper for consignment agreement list queries.</summary>
public sealed class CargoDryConsignmentAgreementPagedResultDto
{
    public List<CargoDryConsignmentAgreementListItemDto> Items    { get; init; } = [];
    public int                                           Total    { get; init; }
    public int                                           Page     { get; init; }
    public int                                           PageSize { get; init; }
}
