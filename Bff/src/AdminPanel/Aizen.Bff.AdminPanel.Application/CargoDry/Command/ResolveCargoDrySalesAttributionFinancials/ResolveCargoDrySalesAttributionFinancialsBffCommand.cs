using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.ResolveCargoDrySalesAttributionFinancials;

public sealed class ResolveCargoDrySalesAttributionFinancialsBffCommand
    : AizenCommand<ResolveCargoDrySalesAttributionFinancialsBffCommandResponse>
{
    public long     SalesAttributionId    { get; init; }
    public decimal  SalePrice             { get; init; }
    public string   CurrencyCode          { get; init; } = default!;
    public decimal? CommissionRateOverride { get; init; }
    public long     ResolvedByUserId      { get; init; }
    public string?  ResolutionNote        { get; init; }
}

public sealed class ResolveCargoDrySalesAttributionFinancialsBffCommandResponse
{
    public CargoDrySalesAttributionBffDto? Attribution { get; init; }
}
