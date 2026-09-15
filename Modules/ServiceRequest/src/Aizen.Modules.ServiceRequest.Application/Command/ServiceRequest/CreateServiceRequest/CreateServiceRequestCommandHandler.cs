using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.ServiceRequest.Abstraction.Constants;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Common;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;
using Aizen.Modules.ServiceRequest.Repository.Persistence;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Create service request command handler", "Creates a new service request, adds initial items if provided, records initial status history, and publishes realtime event.")]
public sealed class CreateServiceRequestCommandHandler : AizenCommandHandler<CreateServiceRequestCommand, CreateServiceRequestResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IServiceRequestReferenceDataRemoteCall _referenceData;
    private readonly ICargoDrySupplyRemoteCall _cargoDry;
    private readonly IPaymentModuleRemoteCall _payment;
    private readonly ServiceRequestDbContext _db;

    public CreateServiceRequestCommandHandler(
        IServiceRequestRepository repository,
        IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher,
        IServiceRequestReferenceDataRemoteCall referenceData,
        ICargoDrySupplyRemoteCall cargoDry,
        IPaymentModuleRemoteCall payment,
        ServiceRequestDbContext db)
    {
        _repository = repository;
        _info = info;
        _realtimePublisher = realtimePublisher;
        _referenceData = referenceData;
        _cargoDry = cargoDry;
        _payment = payment;
        _db = db;
    }

    public override async Task<CreateServiceRequestResponse?> Handle(CreateServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var rawToken      = _info.UserInfoAccessor.UserInfo.AccessToken; // forwarded to CargoDry + Payment
        var req = request.Request;
        var requestCode = $"SR{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var entity = ServiceRequestEntity.Create(
            requestCode, currentUserId, req.VesselId, req.ServiceCategoryCode, req.ServiceTypeCode,
            req.Title, req.Description, req.Priority, req.RequestedStartDate, req.RequestedEndDate,
            req.LocationCountryCode, req.LocationCityCode, req.LocationMarinaName,
            req.LocationLatitude, req.LocationLongitude, req.OwnerNotes, req.ExpiresAt);

        // CargoDry supply v2 — ORDER MODEL: creating a CARGODRY_SUPPLY request charges the owner the retail price
        // into the platform-collected escrow (PrincipalSale, provider split = 0) AT CREATION and immediately publishes
        // the order to the program-provider pool. The product code is category-specific payload (no SR metadata column).
        var isCargoSupply = string.Equals(entity.ServiceCategoryCode, ServiceRequestServiceCategoryCodes.CargoDrySupply, StringComparison.OrdinalIgnoreCase);
        decimal retailPrice = 0m;
        string retailCurrency = "TRY";
        if (isCargoSupply)
        {
            if (string.IsNullOrWhiteSpace(req.CargoDryProductCode))
                throw new AizenBusinessException("CargoDryProductCode is required for a CARGODRY_SUPPLY service request.");
            entity.SetCargoDryProductCode(req.CargoDryProductCode);

            // Authoritative retail + active check from CargoDry (providerProfileId 0 → product read only).
            var ctx = await _cargoDry.GetSupplyAcceptContextAsync(0, req.CargoDryProductCode!.Trim().ToUpperInvariant(), $"Bearer {rawToken}", cancellationToken);
            if (!ctx.ProductActive)
                throw new AizenBusinessException("SR_CARGODRY_PRODUCT_UNAVAILABLE");
            retailPrice = ctx.RetailPrice;
            retailCurrency = string.IsNullOrWhiteSpace(ctx.CurrencyCode) ? "TRY" : ctx.CurrencyCode!;
            if (retailPrice <= 0m)
                throw new AizenBusinessException("SR_CARGODRY_PRODUCT_UNAVAILABLE");
            entity.SetCargoDryRetail(retailPrice, retailCurrency); // frozen for revenue recognition at completion
        }

        if (!string.IsNullOrWhiteSpace(req.LocationCityCode))
        {
            var countryCode = req.LocationCountryCode ?? "TR";
            var cityResult = await _referenceData.GetCity(countryCode, req.LocationCityCode);
            if (cityResult.Body is null || !cityResult.Body.IsActive)
                throw new AizenBusinessException($"Location city code '{req.LocationCityCode}' is not a recognised ReferenceData city.");
        }

        await _repository.AddAsync(entity, cancellationToken);

        int sortOrder = 0;
        foreach (var item in req.Items)
        {
            var itemEntity = ServiceRequestItemEntity.Create(
                entity.Id, item.ItemType, item.Title, item.Description,
                item.Quantity, item.UnitCode, item.EstimatedUnitPrice, sortOrder++);
            entity.AddItem(itemEntity);
        }

        var history = ServiceRequestStatusHistoryEntity.Create(
            entity.Id, ServiceRequestStatus.Draft, ServiceRequestStatus.Draft,
            "Created", currentUserId, ServiceRequestActorType.Owner);
        entity.AddStatusHistory(history);

        // ── CargoDry supply v2: charge escrow at creation, then publish to the provider pool with an accept deadline. ──
        if (isCargoSupply)
        {
            // Flush so the SR id exists for the escrow idempotency key + context. All within the handler transaction,
            // so a downstream failure (incl. the escrow call) rolls the SR back — no orphan order.
            await _db.SaveChangesAsync(cancellationToken);

            var escrow = await _payment.CreateEscrowAsync(new CreateEscrowRemoteCallRequest
            {
                IdempotencyKey                   = $"SR-{entity.Id}-CARGODRY-SUPPLY",
                Context                          = TransactionContext.ForServiceRequest(entity.Id, 0),
                TransactionType                  = TransactionType.CargoDrySupplyEscrow,
                PayerProfileId                   = currentUserId,
                RecipientProfileId               = null,               // PrincipalSale — platform is the merchant
                GrossAmount                      = retailPrice,
                DiscountAmount                   = 0m,
                CurrencyCode                     = retailCurrency,
                ProviderPlanId                   = null,
                CategoryCode                     = entity.ServiceCategoryCode,
                EscrowRequired                   = true,
                PlatformCollectedNoProviderShare = true,              // zero provider split by construction
            }, $"Bearer {rawToken}", cancellationToken);

            entity.SetPaymentTransaction(escrow.TransactionId);

            var acceptHours = await CargoDrySupplyOptions.GetProviderAcceptTimeoutHoursAsync(_referenceData);
            var prevStatus = entity.Status;
            entity.Publish(); // Draft → Open (enters provider discovery)
            entity.SetProviderAcceptDeadline(DateTime.UtcNow.AddHours(acceptHours));
            entity.AddStatusHistory(ServiceRequestStatusHistoryEntity.Create(
                entity.Id, prevStatus, ServiceRequestStatus.Open,
                "CargoDry order created + paid (escrow held); open to program providers", currentUserId, ServiceRequestActorType.Owner));
            _repository.Update(entity);
        }

        await _realtimePublisher.PublishAsync(
            entity.Id, entity.RequestCode, currentUserId, null,
            ServiceRequestRealtimeEventType.ServiceRequestCreated,
            entity.ToDto(), currentUserId, ServiceRequestActorType.Owner, cancellationToken);

        return new CreateServiceRequestResponse(entity.ToDto());
    }
}
