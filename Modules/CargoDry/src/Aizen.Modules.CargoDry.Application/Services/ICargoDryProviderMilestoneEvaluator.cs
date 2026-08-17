namespace Aizen.Modules.CargoDry.Application.Services;

public interface ICargoDryProviderMilestoneEvaluator
{
    Task EvaluateAfterSaleAsync(long providerProfileId, DateTimeOffset occurredAtUtc, CancellationToken ct);
}
