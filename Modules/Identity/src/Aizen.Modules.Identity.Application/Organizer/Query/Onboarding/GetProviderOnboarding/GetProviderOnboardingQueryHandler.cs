using System.Text.Json;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Onboarding.GetProviderOnboarding;

public sealed class GetProviderOnboardingQueryHandler
    : AizenQueryHandler<GetProviderOnboardingQuery, ProviderOnboardingResponse>
{
    private readonly IProviderOnboardingDomainService _service;
    public GetProviderOnboardingQueryHandler(IProviderOnboardingDomainService service) => _service = service;

    public override async Task<ProviderOnboardingResponse?> Handle(
        GetProviderOnboardingQuery request, CancellationToken ct)
    {
        var entity = await _service.GetAsync(request.ProfileId, ct);
        if (entity is null) return null;

        return new ProviderOnboardingResponse
        {
            ProfileId = entity.ProfileId,
            Status = entity.Status.ToString(),
            SchemaVersion = entity.SchemaVersion,
            StepStatuses = entity.GetStepStatuses(),
            Draft = JsonSerializer.Deserialize<JsonElement>(entity.DraftJson ?? "{}"),
            RevisionSteps = string.IsNullOrEmpty(entity.RevisionStepsJson)
                ? null
                : JsonSerializer.Deserialize<string[]>(entity.RevisionStepsJson),
            RevisionNote = entity.RevisionNote,
            LastSavedAtUtc = entity.LastSavedAtUtc,
            SubmittedAtUtc = entity.SubmittedAtUtc,
        };
    }
}
