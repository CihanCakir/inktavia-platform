using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Documents;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Repository.Mongo;

namespace Aizen.Modules.Vessel.Repository.Service;

[DocumentationInfo("Vessel snapshot service", "Builds denormalized VesselDetailDto and maintains the MongoDB read-side snapshot.")]
public sealed class VesselSnapshotService : IVesselSnapshotService
{
    private readonly IVesselRepository _vesselRepository;
    private readonly VesselProfileReadRepository _readRepository;

    public VesselSnapshotService(IVesselRepository vesselRepository, VesselProfileReadRepository readRepository)
    {
        _vesselRepository = vesselRepository;
        _readRepository = readRepository;
    }

    public async Task<VesselDetailDto?> BuildDetailAsync(long vesselId, CancellationToken ct = default)
    {
        var vessel = await _vesselRepository.GetByIdWithDetailsAsync(vesselId, ct);
        if (vessel is null) return null;

        return new VesselDetailDto
        {
            Vessel = new VesselDto
            {
                Id = vessel.Id,
                PublicId = vessel.PublicId,
                VesselCode = vessel.VesselCode,
                Name = vessel.Name,
                Slug = vessel.Slug,
                VesselTypeCode = vessel.VesselTypeCode,
                VesselUsageTypeCode = vessel.VesselUsageTypeCode,
                FlagCountryCode = vessel.FlagCountryCode,
                RegistrationNumber = vessel.RegistrationNumber,
                MmsiNumber = vessel.MmsiNumber,
                ImoNumber = vessel.ImoNumber,
                Status = vessel.Status,
                Visibility = vessel.Visibility,
                IsArchived = vessel.IsArchived,
                ArchiveReason = vessel.ArchiveReason,
                ArchivedAt = vessel.ArchivedAt,
                CreateDate = vessel.CreateDate,
                ModifyDate = vessel.ModifyDate
            }
        };
    }

    public async Task SyncReadDocumentAsync(long vesselId, CancellationToken ct = default)
    {
        var vessel = await _vesselRepository.GetByIdWithDetailsAsync(vesselId, ct);
        if (vessel is null) return;

        var doc = new VesselProfileReadDocument
        {
            VesselId = vessel.Id,
            PublicId = vessel.PublicId,
            VesselCode = vessel.VesselCode,
            Name = vessel.Name,
            Slug = vessel.Slug,
            VesselTypeCode = vessel.VesselTypeCode,
            FlagCountryCode = vessel.FlagCountryCode,
            Status = vessel.Status,
            Visibility = vessel.Visibility,
            IsArchived = vessel.IsArchived,
            OwnerUserIds = vessel.Owners.Where(o => o.IsActive).Select(o => o.UserId).ToList(),
            CoverMediaUrl = vessel.Media.FirstOrDefault(m => m.IsCover && m.IsActive)?.FileUrl,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = vessel.IsDeleted,
        };

        await _readRepository.UpsertAsync(doc, ct);
    }
}
