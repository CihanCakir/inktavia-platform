using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Model;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Abstraction.Response.Engine;

[DocumentationInfo("Get vessel engines response", "Returns a paged list of vessel engines.")]
public sealed record GetVesselEnginesResponse(Paginate<VesselEngineDto> Engines);
