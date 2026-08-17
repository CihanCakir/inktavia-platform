using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Domain.Service;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Organizer;

public sealed class GetOrganizerProfileDetailQueryHandler
    : AizenQueryHandler<GetOrganizerProfileDetailQuery, OrganizerProfileDetailDto>
{
    private readonly IdentityDbContext _db;

    public GetOrganizerProfileDetailQueryHandler(IdentityDbContext db)
    {
        _db = db;
    }

    public override async Task<OrganizerProfileDetailDto> Handle(GetOrganizerProfileDetailQuery request, CancellationToken cancellationToken)
    {
        var profile = await _db.UserProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.VerificationDocuments)
            .Include(p => p.RiskSignals)
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId
                && p.RoleContext == WorkshopRoleContext.Organizer
                && !p.IsDeleted, cancellationToken)
            ?? throw new AizenBusinessException(((int)AizenErrorCode.NotFound).ToString());

        var dto = new OrganizerProfileDetailDto
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

        return dto;
    }
}
