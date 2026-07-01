using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Queries.GetPaymentDashboardKpis;

public sealed class GetPaymentDashboardKpisQuery : AizenQuery<PaymentDashboardKpisDto> { }

public sealed record PaymentDashboardKpisDto(
    decimal GrossVolumeToday,
    decimal GrossVolumeChange,
    decimal PlatformCommissionToday,
    decimal CommissionChange,
    decimal PayoutPendingTotal,
    decimal PayoutChange,
    decimal HeldInEscrow,
    decimal EscrowChange,
    int     PendingTransactionCount,
    int     FailedTransactionCount
);
