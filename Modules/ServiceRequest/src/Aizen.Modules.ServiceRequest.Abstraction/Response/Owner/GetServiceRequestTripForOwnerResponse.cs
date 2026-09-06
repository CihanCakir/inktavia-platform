using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;

[DocumentationInfo("Get service request trip for owner response", "The current live trip for an owned SR, or null when there is none.")]
public sealed record GetServiceRequestTripForOwnerResponse(TripDto? Trip);
