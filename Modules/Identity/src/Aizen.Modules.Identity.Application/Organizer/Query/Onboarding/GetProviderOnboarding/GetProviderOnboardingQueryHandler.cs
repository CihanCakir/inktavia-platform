using System.Text.Json;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Onboarding.GetProviderOnboarding;

public sealed class GetProviderOnboardingQueryHandler
    : AizenQueryHandler<GetProviderOnboardingQuery, ProviderOnboardingResponse>
{
    private readonly IProviderOnboardingDomainService _service;
    private readonly IdentityDbContext _db;

    public GetProviderOnboardingQueryHandler(IProviderOnboardingDomainService service, IdentityDbContext db)
    {
        _service = service;
        _db = db;
    }

    public override async Task<ProviderOnboardingResponse?> Handle(
        GetProviderOnboardingQuery request, CancellationToken ct)
    {
        var entity = await _service.GetAsync(request.ProfileId, ct);
        if (entity is null) return null;

        var documents = await _db.VerificationDocuments
            .Where(d => d.ProfileId == request.ProfileId && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new ProviderDocumentDto
            {
                FileId = d.FilePublicId ?? Guid.Empty,
                FileName = d.Name,
                ContentType = d.ContentType ?? d.Format,
                SizeInBytes = d.SizeInBytes,
                DocumentType = d.DocumentType,
                Issuer = d.Issuer,
                UploadedAt = d.UploadedAt,
            })
            .ToListAsync(ct);

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
            Documents = documents,
        };
    }
}
