using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Repository.Service;

[DocumentationInfo("Vessel cache key service", "Centralises cache key computation for all vessel-related queries.")]
public sealed class VesselCacheKeyService : IVesselCacheKeyService
{
    public string VesselDetail(long vesselId) => $"vessel:detail:{vesselId}";
    public string VesselById(long vesselId) => $"vessel:id:{vesselId}";
    public string VesselByCode(string vesselCode) => $"vessel:code:{vesselCode}";
    public string UserVesselList(long userId) => $"vessel:user:{userId}:list";
    public string VesselOwners(long vesselId) => $"vessel:{vesselId}:owners";
    public string VesselSpecification(long vesselId) => $"vessel:{vesselId}:specification";
    public string VesselEngines(long vesselId) => $"vessel:{vesselId}:engines";
    public string VesselDocuments(long vesselId) => $"vessel:{vesselId}:documents";
    public string VesselMedia(long vesselId) => $"vessel:{vesselId}:media";
    public string CurrentLocation(long vesselId) => $"vessel:{vesselId}:location:current";
    public string StatusHistory(long vesselId) => $"vessel:{vesselId}:status-history";
}
