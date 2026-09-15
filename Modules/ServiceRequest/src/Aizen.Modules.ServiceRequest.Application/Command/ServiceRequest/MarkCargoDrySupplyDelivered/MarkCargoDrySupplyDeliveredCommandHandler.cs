using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Constants;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Common;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest.MarkCargoDrySupplyDelivered;

[DocumentationInfo("Mark CargoDry supply delivered command handler",
    "Assigned provider marks a CARGODRY_SUPPLY order delivered (capturing the kit) and starts the delivered " +
    "auto-complete window. Gated to the assigned provider.")]
public sealed class MarkCargoDrySupplyDeliveredCommandHandler
    : AizenCommandHandler<MarkCargoDrySupplyDeliveredCommand, MarkCargoDrySupplyDeliveredResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;
    private readonly IServiceRequestReferenceDataRemoteCall _referenceData;
    private readonly ILogger<MarkCargoDrySupplyDeliveredCommandHandler> _logger;

    public MarkCargoDrySupplyDeliveredCommandHandler(
        IServiceRequestRepository repository,
        IAizenInfoAccessor info,
        IServiceRequestReferenceDataRemoteCall referenceData,
        ILogger<MarkCargoDrySupplyDeliveredCommandHandler> logger)
    {
        _repository = repository;
        _info = info;
        _referenceData = referenceData;
        _logger = logger;
    }

    public override async Task<MarkCargoDrySupplyDeliveredResponse?> Handle(
        MarkCargoDrySupplyDeliveredCommand request, CancellationToken cancellationToken)
    {
        if (request.KitId <= 0)
            throw new AizenBusinessException("A delivered kit must be specified.");

        var sr = await _repository.GetByIdWithDetailsAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        if (!string.Equals(sr.ServiceCategoryCode, ServiceRequestServiceCategoryCodes.CargoDrySupply, StringComparison.OrdinalIgnoreCase))
            throw new AizenBusinessException("This is not a CargoDry supply order.");

        // Gate to the assigned provider (identity from the trusted token, never the body).
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0 || sr.Assignment is null || sr.Assignment.ProviderProfileId != providerProfileId)
            throw new AizenBusinessException("Only the assigned provider can mark this order delivered.");

        if (sr.Status != ServiceRequestStatus.Assigned)
            throw new AizenBusinessException("Only an assigned order can be marked delivered.");

        var hours = await CargoDrySupplyOptions.GetDeliveredAutoCompleteHoursAsync(_referenceData);
        var now = DateTime.UtcNow;
        var deadline = now.AddHours(hours);

        sr.MarkDelivered(request.KitId, now, deadline);
        sr.AddStatusHistory(ServiceRequestStatusHistoryEntity.Create(
            sr.Id, sr.Status, sr.Status,
            $"CargoDry kit {request.KitId} delivered; auto-completes at {deadline:o} if not activated",
            _info.UserInfoAccessor.UserInfo.UserId, ServiceRequestActorType.Provider));
        _repository.Update(sr);

        _logger.LogInformation(
            "CargoDry supply SR {SrId} marked delivered (kit {KitId}); auto-complete deadline {Deadline:o}.",
            sr.Id, request.KitId, deadline);

        return new MarkCargoDrySupplyDeliveredResponse
        {
            ServiceRequestId = sr.Id,
            DeliveredAtUtc = now,
            AutoCompleteDeadlineUtc = deadline,
        };
    }
}
