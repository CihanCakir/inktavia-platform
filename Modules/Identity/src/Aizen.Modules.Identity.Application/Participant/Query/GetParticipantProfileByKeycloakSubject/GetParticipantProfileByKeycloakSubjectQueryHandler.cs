using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Participant;

/// <summary>
/// Resolves a Participant profile by Keycloak subject and maps it to the shared profile detail DTO.
/// Mirrors <c>GetOrganizerProfileByKeycloakSubjectQueryHandler</c>; null when unlinked.
/// </summary>
public sealed class GetParticipantProfileByKeycloakSubjectQueryHandler
    : AizenQueryHandler<GetParticipantProfileByKeycloakSubjectQuery, OrganizerProfileDetailDto>
{
    private readonly IUserProfileRepository _profiles;

    public GetParticipantProfileByKeycloakSubjectQueryHandler(IUserProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public override async Task<OrganizerProfileDetailDto?> Handle(
        GetParticipantProfileByKeycloakSubjectQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetParticipantProfileByKeycloakSubjectAsync(request.KeycloakSubject, cancellationToken);
        if (profile is null)
            return null;

        return new OrganizerProfileDetailDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Gender = profile.Gender,
            BirthDate = profile.BirthDate,
            Bio = profile.Bio,
            ProfilePhotoUrl = profile.ProfilePhotoUrl,
            NationalityId = profile.NationalityId,
            TaxpayerType = profile.TaxpayerType.ToString(),
            ApprovalStatus = profile.ApprovalStatus.ToString(),
            Status = profile.Status.ToString(),
            ApprovedAt = profile.ApprovedAt,
            RejectedAt = profile.RejectedAt,
            RejectReason = profile.RejectReason,
            CreateDate = profile.CreateDate,
            ModifyDate = profile.ModifyDate,
            OrganizationName = profile.CompanyName,
            Email = profile.User?.Email,
            Phone = profile.User?.PhoneNumber,
            PreferredLanguage = profile.User?.PreferredLanguage,
            City = profile.City,
            Country = profile.Country,
            ReviewedBy = profile.ReviewedBy,
            ReviewedAt = profile.ReviewedAt?.ToString("O"),
            RejectionCategory = profile.RejectionCategory,
            RiskLevel = RiskAssessmentService.ComputeRiskLevel(profile.RiskSignals),
            Documents = profile.VerificationDocuments
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new VerificationDocumentDto
                {
                    Id = d.Id,
                    FileId = d.FilePublicId ?? Guid.Empty,
                    Name = d.Name,
                    DocumentType = d.DocumentType,
                    Format = d.Format,
                    FileSizeDisplay = d.FileSizeDisplay,
                    Issuer = d.Issuer,
                    MatchScore = d.MatchScore,
                    UploadedAt = d.UploadedAt.ToString("O")
                })
                .ToList(),
            RiskSignals = profile.RiskSignals
                .Select(s => new RiskSignalDto
                {
                    Severity = s.Severity,
                    Title = s.Title,
                    Description = s.Description,
                    SignalCode = s.SignalCode,
                    DetectedAt = s.DetectedAt.ToString("O")
                })
                .ToList()
        };
    }
}
