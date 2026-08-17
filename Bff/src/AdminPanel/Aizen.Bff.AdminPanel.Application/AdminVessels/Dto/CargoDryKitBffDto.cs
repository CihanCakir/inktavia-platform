using System.Text.Json.Serialization;
using Aizen.Bff.AdminPanel.Application.Common;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;

[DocumentationInfo("CargoDry kit BFF DTO", "Full CargoDry kit data for the admin panel and vessel detail aggregation.")]
public sealed class CargoDryKitBffDto
{
    public long    Id                { get; init; }
    public string  SerialNumber      { get; init; } = default!;
    public string  KitCode           { get; init; } = default!;
    public string  ProductCode       { get; init; } = default!;
    public string  ProductName       { get; init; } = default!;
    public string  BatchCode         { get; init; } = default!;
    public string  Status            { get; init; } = default!;
    public long?   OwnerUserId       { get; init; }
    public string? OwnerDisplayName  { get; init; }
    public long?   VesselId          { get; init; }
    public string? VesselName        { get; init; }

    // Backend sends "activatedAt" / "expiresAt" (Newtonsoft camelCase of ActivatedAt/ExpiresAt).
    // STJ [JsonPropertyName] maps the Refit deserialization key without affecting
    // the Newtonsoft serialization key sent to the frontend (still "activatedDate"/"expiryDate").
    [JsonPropertyName("activatedAt")]
    public string? ActivatedDate     { get; init; }

    [JsonPropertyName("expiresAt")]
    public string? ExpiryDate        { get; init; }

    public double  EfficiencyPercent { get; init; }
    public int     DaysUntilExpiry   { get; init; }
    public int     RenewalCount      { get; init; }
    public string  ManufacturedAt    { get; init; } = default!;
}

[DocumentationInfo("CargoDry kit list BFF DTO", "Paginated admin kit list response.")]
public sealed class CargoDryKitListBffDto
{
    public List<CargoDryKitBffDto> Items    { get; init; } = [];
    public int                     Total    { get; init; }
    public int                     Page     { get; init; }
    public int                     PageSize { get; init; }
}

[DocumentationInfo("CargoDry stats BFF DTO", "Aggregated kit statistics for the admin dashboard.")]
public sealed class CargoDryStatsBffDto
{
    public int    TotalKits          { get; init; }
    public int    AvailableKits      { get; init; }
    public int    ActiveKits         { get; init; }
    public int    ExpiringKits       { get; init; }
    public int    ExpiredKits        { get; init; }
    public int    RevokedKits        { get; init; }
    public int    TodayActivations   { get; init; }
    public int    TotalBatches       { get; init; }
    public double RenewalRatePercent { get; init; }
}

[DocumentationInfo("CargoDry validation BFF DTO", "QR / serial validation result returned to the onboarding flow.")]
public sealed class CargoDryValidationBffDto
{
    public bool    IsValid         { get; init; }
    public string? InvalidReason   { get; init; }
    public string? ProductName     { get; init; }
    public string? ProductCode     { get; init; }
    public int     ValidityDays    { get; init; }
    public bool    HasSmartDevice  { get; init; }
    public string? ActivationToken { get; init; }
    public string? TokenExpiresAt  { get; init; }
}

[DocumentationInfo("Generate batch BFF result DTO", "Returned after admin generates a new kit batch.")]
public sealed class GenerateBatchBffResultDto
{
    public string BatchCode      { get; init; } = default!;
    public int    GeneratedCount { get; init; }
    public string QrZipFileUrl   { get; init; } = default!;
    public string ExcelFileUrl   { get; init; } = default!;
}

/// <summary>Returned by revoke endpoint after REV-E (replaces bool)</summary>
public sealed class RevokeKitBffResponse
{
    public long   KitId        { get; init; }
    public string KitCode      { get; init; } = default!;
    public string SerialNumber { get; init; } = default!;
    public string Status       { get; init; } = default!;
    public string Reason       { get; init; } = default!;
    public string RevokedAt    { get; init; } = default!;
}

/// <summary>Product catalog item — GET /admin/products</summary>
public sealed class CargoDryProductBffDto
{
    public long    Id             { get; init; }
    public string  ProductCode    { get; init; } = default!;
    public string  Name           { get; init; } = default!;
    public string? Description    { get; init; }
    public int     ValidityDays   { get; init; }
    public bool    HasSmartDevice { get; init; }
    public decimal RetailPrice    { get; init; }
    public string  CurrencyCode   { get; init; } = default!;
    public bool    IsActive       { get; init; }
}

/// <summary>Batch summary for batch history list</summary>
public sealed class CargoDryBatchBffDto
{
    public long    Id               { get; init; }
    public string  BatchCode        { get; init; } = default!;
    public string  ProductCode      { get; init; } = default!;
    public string  ProductName      { get; init; } = default!;
    public int     TotalKits        { get; init; }
    public string  GeneratedAt      { get; init; } = default!;
    public bool    IsRevoked        { get; init; }
    public string? QrZipFileRef     { get; init; }
    public string? ExcelFileRef     { get; init; }
    public long    CreatedByAdminId { get; init; }
}

public sealed class CargoDryBatchListBffDto
{
    public List<CargoDryBatchBffDto> Items    { get; init; } = [];
    public int                       Total    { get; init; }
    public int                       Page     { get; init; }
    public int                       PageSize { get; init; }
}
