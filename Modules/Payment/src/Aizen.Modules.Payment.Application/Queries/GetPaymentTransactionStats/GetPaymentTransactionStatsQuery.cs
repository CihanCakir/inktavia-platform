using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.GetPaymentTransactionStats;

public sealed class GetPaymentTransactionStatsQuery : AizenQuery<PaymentTransactionStatsDto> { }

public sealed record PaymentTransactionStatsDto(
    decimal NetLiquidity,
    decimal NetLiquidityChange,
    decimal PendingClearances,
    decimal PendingClearancesChange,
    decimal OperationalBurn,
    decimal OperationalBurnChange,
    decimal FleetRoi,
    decimal FleetRoiChange
);
