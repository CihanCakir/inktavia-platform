using System.Globalization;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.Payment.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.Payment;

/// <summary>
/// N4 (§19.7) — when a customer-benefit budget crosses the low/exhausted threshold, alert every admin so ops can top up
/// before customer discounts silently stop. Resolves admin user ids from Identity, sends one
/// <see cref="NotificationType.BenefitBudgetLow"/> (Payments category, N-B gated) per admin. Requires the seeded
/// <c>BENEFIT_BUDGET_LOW_INAPP</c> template.
/// </summary>
public sealed class CustomerBenefitBudgetLowConsumer
    : AizenBaseMessageConsumer<CustomerBenefitBudgetLowMessage>
{
    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly ILogger<CustomerBenefitBudgetLowConsumer> _logger;

    public CustomerBenefitBudgetLowConsumer(IServiceProvider sp) : base(sp)
    {
        _sender   = sp.GetRequiredService<ISender>();
        _identity = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _logger   = sp.GetRequiredService<ILogger<CustomerBenefitBudgetLowConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(CustomerBenefitBudgetLowMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(CustomerBenefitBudgetLowMessage message, CancellationToken ct)
    {
        List<long> adminIds;
        try
        {
            var response = await _identity.GetAdminUserIds();
            adminIds = response.Body ?? new List<long>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not resolve admin user ids for BenefitBudgetLow (budget {BudgetId}); admins not notified.",
                message.BudgetId);
            return;
        }

        if (adminIds.Count == 0)
        {
            _logger.LogWarning("No admin users resolved; BenefitBudgetLow notification skipped for budget {BudgetId}.",
                message.BudgetId);
            return;
        }

        var vars = new Dictionary<string, string>
        {
            ["plan"]      = message.CustomerPlanId.ToString(),
            ["remaining"] = message.RemainingAmount.ToString("F2", CultureInfo.InvariantCulture),
            ["funded"]    = message.FundedAmount.ToString("F2", CultureInfo.InvariantCulture),
            ["percent"]   = message.ThresholdPercent.ToString("0.#", CultureInfo.InvariantCulture),
            ["currency"]  = message.CurrencyCode,
        };
        var metadata = $"{{\"budgetId\":{message.BudgetId},\"customerPlanId\":{message.CustomerPlanId},\"isExhausted\":{message.IsExhausted.ToString().ToLowerInvariant()}}}";

        foreach (var adminId in adminIds)
        {
            await _sender.Send(new SendNotificationCommand
            {
                RecipientUserId = adminId,
                Type            = NotificationType.BenefitBudgetLow,
                Channel         = NotificationChannel.InApp,
                Variables       = new Dictionary<string, string>(vars),
                MetadataJson    = metadata,
                ReferenceType   = "CustomerBenefitBudget",
                ReferenceId     = message.BudgetId,
            }, ct);
        }

        _logger.LogInformation(
            "BenefitBudgetLow budget={BudgetId} remaining={Remaining} (exhausted={Exhausted}) → notified {Count} admins.",
            message.BudgetId, message.RemainingAmount, message.IsExhausted, adminIds.Count);
    }

    public override Task ExecuteRollbackMessage(CustomerBenefitBudgetLowMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: CustomerBenefitBudgetLowConsumer budget={BudgetId}: {Error}", message.BudgetId, ex.Message);
        return Task.CompletedTask;
    }
}
