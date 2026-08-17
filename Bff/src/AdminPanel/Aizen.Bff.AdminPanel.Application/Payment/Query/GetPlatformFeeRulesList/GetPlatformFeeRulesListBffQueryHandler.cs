using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPlatformFeeRulesList;

[DocumentationInfo("Get platform-fee rules list BFF query handler (BE-P3)",
    "Returns a paged list of platform-fee rules with optional Model/Status/Currency/Category/CustomerType filters, " +
    "forwarded as plain strings to the Payment module (enum parsing handled by the module's own converter). Read-only.")]
public sealed class GetPlatformFeeRulesListBffQueryHandler
    : AizenQueryHandler<GetPlatformFeeRulesListBffQuery, GetPlatformFeeRulesListBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetPlatformFeeRulesListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetPlatformFeeRulesListBffResponse?> Handle(GetPlatformFeeRulesListBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ListPlatformFeeRulesAsync(
            request.Model, request.Status, request.CurrencyCode, request.CategoryCode, request.CustomerType,
            request.Page, request.PageSize, ct) };
}
