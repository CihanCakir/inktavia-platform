using Aizen.Bff.AdminPanel.Application.AdminFinance.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Query.GetFinancialSummaryReportBff;

[DocumentationInfo("Get financial summary report BFF query handler (BE-P12)",
    "Proxies the admin financial-summary request to the Payment finance endpoint. Returns the §15 period summary " +
    "(NetMarketplaceContribution + per-line breakdown; VAT liability + provider-funded discount shown separately). " +
    "No financial calculations are performed in the BFF.")]
public sealed class GetFinancialSummaryReportBffQueryHandler
    : AizenQueryHandler<GetFinancialSummaryReportBffQuery, FinancialSummaryReportBffDto>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public GetFinancialSummaryReportBffQueryHandler(IAdminPaymentBffRemoteCall remote) => _remote = remote;

    public override async Task<FinancialSummaryReportBffDto> Handle(
        GetFinancialSummaryReportBffQuery request, CancellationToken ct)
        => await _remote.GetFinancialSummaryReportAsync(request.From, request.To, request.Currency, ct);
}
