using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPartCommercialTermsList;

[DocumentationInfo("Get part commercial terms list BFF query handler (BE-S5)",
    "Returns the part commercial term list (no paging) with optional brand / productCode / provider / category / currency / " +
    "active filters, forwarded to the Payment module. Admin-only surface — carries confidential cost. Read-only.")]
public sealed class GetPartCommercialTermsListBffQueryHandler
    : AizenQueryHandler<GetPartCommercialTermsListBffQuery, GetPartCommercialTermsListBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public GetPartCommercialTermsListBffQueryHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<GetPartCommercialTermsListBffResponse?> Handle(GetPartCommercialTermsListBffQuery request, CancellationToken ct)
        => new() { Result = await _payment.ListPartCommercialTermsAsync(
            request.Brand, request.ProductCode, request.ProviderProfileId, request.CategoryCode, request.CurrencyCode, request.IsActive, ct) };
}
