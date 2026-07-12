using Aizen.Modules.Identity.Domain.Entities.Onboarding;

namespace Aizen.Modules.Identity.Domain.Interface.Service;

public interface IProviderOnboardingDomainService
{
    Task<ProviderOnboardingEntity?> GetAsync(long profileId, CancellationToken ct);
    Task SaveStepAsync(long profileId, string step, string stepStatus, string stepDataJson, int schemaVersion, CancellationToken ct);
    Task SubmitAsync(long profileId, CancellationToken ct);
    Task RequestRevisionAsync(long profileId, string[] steps, string note, CancellationToken ct);
    Task EnsureOnboardingRowAsync(long profileId, long userId, CancellationToken ct);
}
