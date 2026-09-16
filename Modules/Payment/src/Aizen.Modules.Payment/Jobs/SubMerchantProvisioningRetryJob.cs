using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Scheduler;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Payment.Jobs;

/// <summary>
/// Hourly retry sweep for async sub-merchant provisioning. Re-publishes <see cref="ProviderSubMerchantProvisioningRequested"/>
/// for DataSubmitted profiles that are still under the attempt cap and whose last attempt is null or older than the retry
/// window. Beyond the cap the profile is kept but no longer auto-retried (admin can re-trigger, which resets the counter).
/// Config: <c>Payment:SubMerchant:RetryMinutes</c> (60), <c>Payment:SubMerchant:MaxAttempts</c> (10).
/// </summary>
public sealed class SubMerchantProvisioningRetryJob : AizenRecurringJob
{
    private const int BatchSize = 200;

    public SubMerchantProvisioningRetryJob(IAizenSchedulerLogger logger, IServiceProvider serviceProvider)
        : base(logger, serviceProvider) { }

    public override bool   IsActive       => true;
    public override string CronExpression => "0 * * * *"; // every hour, top of the hour (UTC)

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = ServiceProvider.CreateScope();
        var config    = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var profiles  = scope.ServiceProvider.GetRequiredService<IProviderPaymentProfileRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IAizenMessagePublisher>();

        var retryMinutes = config.GetValue<int>("Payment:SubMerchant:RetryMinutes", 60);
        var maxAttempts  = config.GetValue<int>("Payment:SubMerchant:MaxAttempts", 10);
        var retryBefore  = DateTime.UtcNow.AddMinutes(-Math.Max(1, retryMinutes));

        var dueIds = await profiles.GetProvisioningRetryDueIdsAsync(retryBefore, maxAttempts, BatchSize, ct);
        Logger.WriteConsole($"SubMerchantProvisioningRetryJob: {dueIds.Count} profile(s) due for re-provisioning.");

        foreach (var providerProfileId in dueIds)
        {
            await publisher.PublishAsync(new ProviderSubMerchantProvisioningRequested
            {
                ProviderProfileId = providerProfileId,
                Source            = "retry-sweep",
            }, ct);
        }
    }
}
