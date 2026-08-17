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
                ReviewStatus = d.ReviewStatus.ToString(),
                ResolutionNote = d.ResolutionNote,
            })
            .ToListAsync(ct);

        return new ProviderOnboardingResponse
        {
            ProfileId = entity.ProfileId,
            Status = entity.Status.ToString(),
            SchemaVersion = entity.SchemaVersion,
            StepStatuses = entity.GetStepStatuses(),
            // Hand the stored JSON across the wire verbatim. Re-materialising it as a JsonElement only to have
            // Newtonsoft serialise the response would corrupt it — the two serializers do not understand each
            // other's types.
            DraftJson = entity.DraftJson ?? "{}",
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
