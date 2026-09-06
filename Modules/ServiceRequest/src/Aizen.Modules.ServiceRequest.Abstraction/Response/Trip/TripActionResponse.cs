using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Trip;

[DocumentationInfo("Trip action response", "Returns the trip snapshot after a provider start/location/arrive/cancel action.")]
public sealed record TripActionResponse(TripDto Trip);
