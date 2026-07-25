using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderTransactions;

public sealed class GetProviderTransactionsQueryHandler
    : AizenQueryHandler<GetProviderTransactionsQuery, ProviderTransactionPagedResultDto>
{
    private readonly IPaymentTransactionRepository _repo;

    public GetProviderTransactionsQueryHandler(IPaymentTransactionRepository repo) => _repo = repo;

    public override async Task<ProviderTransactionPagedResultDto?> Handle(
        GetProviderTransactionsQuery request, CancellationToken ct)
    {
        var skip   = (request.Page - 1) * request.PageSize;
        var status = request.Status.HasValue ? (PaymentTransactionStatus?)request.Status.Value : null;
        var type   = request.Type.HasValue   ? (TransactionType?)request.Type.Value           : null;

        var (items, total) = await _repo.GetProviderPagedAsync(
            request.ProviderProfileId, status, type, skip, request.PageSize, ct);

        return new ProviderTransactionPagedResultDto
        {
            Items = items.Select(x => new ProviderTransactionDto
            {
                TransactionCode    = x.TransactionCode,
                TransactionType    = (int)x.TransactionType,
                ContextType        = (int)x.ContextType,
                ContextId          = x.ContextId,
                GrossAmount        = x.GrossAmount,
                CommissionAmount   = x.CommissionAmount,
                VatOnCommission    = x.VatOnCommission,
                NetPayoutAmount    = x.NetPayoutAmount,
                TotalRefundedAmount = x.TotalRefundedAmount,
                CurrencyCode       = x.CurrencyCode,
                Status             = (int)x.Status,
                GatewayReference   = x.GatewayReference,
                CreatedAt          = x.CreateDate ?? DateTime.MinValue,
                CapturedAt         = x.CapturedAt,
                ReleasedAt         = x.ReleasedAt,
            }).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }
}
