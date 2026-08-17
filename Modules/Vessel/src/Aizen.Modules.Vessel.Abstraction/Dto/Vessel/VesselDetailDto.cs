using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Dto.Location;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;
using Aizen.Modules.Vessel.Abstraction.Dto.Specification;
using Aizen.Modules.Vessel.Abstraction.Dto.Status;

namespace Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

[DocumentationInfo("Vessel detail DTO", "Aggregates vessel profile, owners, specification, engines, documents, media, location snapshot and status history.")]
public sealed class VesselDetailDto
{
    public VesselDto Vessel { get; set; } = default!;
    public IReadOnlyList<VesselOwnerDto> Owners { get; set; } = [];
    public VesselSpecificationDto? Specification { get; set; }
    public IReadOnlyList<VesselEngineDto> Engines { get; set; } = [];
    public IReadOnlyList<VesselDocumentDto> Documents { get; set; } = [];
    public IReadOnlyList<VesselMediaDto> Media { get; set; } = [];
    public VesselLocationSnapshotDto? CurrentLocation { get; set; }
    public IReadOnlyList<VesselStatusHistoryDto> StatusHistory { get; set; } = [];
}
