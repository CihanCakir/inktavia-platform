using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Query.Admin;
using Aizen.Modules.ServiceRequest.Application.Query.ServiceRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ServiceRequest.Controller.V1.Admin;

[ApiController]
[Route("api/v1/admin/service-requests")]
[Tags("Admin - ServiceRequest")]
[Authorize(Roles = "Admin")]
[DocumentationInfo("Admin ServiceRequest endpoints", "Admin-level service request management and oversight.")]
public sealed class AdminServiceRequestController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminServiceRequestController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetAdminServiceRequestListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetAdminServiceRequestListResponse?>> GetList(
        [FromQuery] AdminServiceRequestFilterRequest filter, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetAdminServiceRequestListResponse>(
            new GetAdminServiceRequestListQuery(filter), ct);
        return SetResponse(result);
    }

    [HttpGet("disputes")]
    [ProducesResponseType(typeof(GetAdminDisputeListResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetAdminDisputeListResponse?>> GetDisputeList(
        [FromQuery] AdminDisputeFilterRequest filter, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetAdminDisputeListResponse>(
            new GetAdminDisputeListQuery(filter), ct);
        return SetResponse(result);
    }

    [HttpGet("{serviceRequestId:long}")]
    [ProducesResponseType(typeof(GetServiceRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetServiceRequestDetailResponse?>> GetDetail(
        [FromRoute] long serviceRequestId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetServiceRequestDetailResponse>(
            new GetServiceRequestDetailQuery(serviceRequestId), ct);
        return SetResponse(result);
    }
}
