using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Dto;

/// <summary>Full detail DTO for a consignment agreement (BFF layer).</summary>
[DocumentationInfo("CargoDry consignment agreement detail BFF DTO",
    "Passes through CargoDryConsignmentAgreementDto from the CargoDry module. " +
    "Phase 1 — CargoDry commercial foundation (July 2026).")]
public sealed class ConsignmentAgreementBffDto
{
    public long   Id                { get; init; }
    public string AgreementCode     { get; init; } = default!;
    public long   ProviderProfileId { get; init; }
    public string ProductCode       { get; init; } = default!;

    public decimal ConsignmentRate          { get; init; }
    public decimal MinimumSettlementAmount  { get; init; }
    public string  CurrencyCode             { get; init; } = default!;
    public int     MaxKitCount              { get; init; }
    public int     AllocatedKitCount        { get; init; }
    public int     RemainingKitCount        { get; init; }

    public ConsignmentAgreementStatus Status       { get; init; }
    public string                     StatusName   { get; init; } = default!;
    public DateTime                   StartDateUtc { get; init; }
    public DateTime?                  EndDateUtc   { get; init; }

    public string? TermsDocumentRef    { get; init; }
    public string? Notes               { get; init; }
    public DateTime? ActivatedAtUtc    { get; init; }
    public DateTime? SuspendedAtUtc    { get; init; }
    public DateTime? TerminatedAtUtc   { get; init; }
    public string?  SuspendReason      { get; init; }
    public string?  TerminationReason  { get; init; }
    public string?  CreatedAt          { get; init; }
    public string?  UpdatedAt          { get; init; }
}

/// <summary>Lightweight list-item DTO for paged agreement list (BFF layer).</summary>
public sealed class ConsignmentAgreementListItemBffDto
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

/// <summary>Paged result wrapper for consignment agreement list (BFF layer).</summary>
public sealed class ConsignmentAgreementPagedBffDto
{
    public List<ConsignmentAgreementListItemBffDto> Items    { get; init; } = [];
    public int                                      Total    { get; init; }
    public int                                      Page     { get; init; }
    public int                                      PageSize { get; init; }
}
