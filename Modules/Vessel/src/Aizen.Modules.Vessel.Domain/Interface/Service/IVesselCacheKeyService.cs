
namespace Aizen.Modules.Vessel.Domain.Interface.Service;

[DocumentationInfo("Vessel cache key service interface", "Centralises cache key computation for all vessel-related queries.")]
public interface IVesselCacheKeyService
{
    string VesselDetail(long vesselId);
    string VesselById(long vesselId);
    string VesselByCode(string vesselCode);
    string UserVesselList(long userId);
    string VesselOwners(long vesselId);
    string VesselSpecification(long vesselId);
    string VesselEngines(long vesselId);
    string VesselDocuments(long vesselId);
    string VesselMedia(long vesselId);
    string CurrentLocation(long vesselId);
    string StatusHistory(long vesselId);
}
