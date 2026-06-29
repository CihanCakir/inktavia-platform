using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.ExportCargoDryKits;

public sealed class ExportCargoDryKitsBffQuery : AizenQuery<CargoDryExportBffResponse>
{
    public string? Status    { get; init; }
    public string? Search    { get; init; }
    public long?   VesselId  { get; init; }
    public string? BatchCode { get; init; }
}
