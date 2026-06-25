using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel detail BFF query handler", "Aggregates vessel detail with service history for the Vessel Detail Overview page. CargoDry module not yet integrated.")]
public sealed class GetAdminVesselDetailBffQueryHandler
    : AizenQueryHandler<GetAdminVesselDetailBffQuery, AdminVesselDetailBffResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminVesselDetailBffQueryHandler(
        IVesselAdminBffRemoteCall vessel,
        IServiceRequestAdminBffRemoteCall serviceRequest,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _vessel = vessel;
        _serviceRequest = serviceRequest;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminVesselDetailBffResponse?> Handle(
        GetAdminVesselDetailBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselDetailBffResponse();

        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

        var vesselTask = _vessel.GetVesselById(request.VesselId, authHeader, request.UserToken);
        var serviceHistoryTask = _serviceRequest.GetAdminServiceRequestList(
            authHeader, request.UserToken, vesselId: request.VesselId, pageIndex: 0, pageSize: 10);

        await Task.WhenAll(
            vesselTask.ContinueWith(_ => { }),
            serviceHistoryTask.ContinueWith(_ => { }));

        if (!vesselTask.IsCompletedSuccessfully || vesselTask.Result?.Body?.Vessel == null)
        {
            response.Warnings.Add(AdminBffWarning.CallFailed("Vessel.Detail", "Could not retrieve vessel detail."));
            return response;
        }

        var v = vesselTask.Result.Body.Vessel;
        var spec = v.Specification;
        var primaryEngine = v.Engines?.FirstOrDefault(e => e.IsPrimary);
        var currentLocation = v.CurrentLocation;

        response.Vessel = new VesselDetailBffDto
        {
            Id = v.Vessel.Id,
            VesselCode = v.Vessel.VesselCode,
            Name = v.Vessel.Name,
            Slug = v.Vessel.Slug,
            VesselTypeCode = v.Vessel.VesselTypeCode,
            FlagCountryCode = v.Vessel.FlagCountryCode,
            HeroImageUrl = null, // populated by FileStorage integration (future)
            YearBuilt = spec?.ProductionYear,
            BuilderName = spec?.Brand,
            BuildCountry = spec?.BuildCountry,
            HullMaterial = spec?.HullMaterialCode,
            SuperstructureMaterial = spec?.SuperstructureMaterial,
            BeamMeters = spec?.BeamValue,
            DraftMeters = spec?.DraftValue,
            LengthMeters = spec?.LengthValue,
            GrossTonnage = spec?.GrossTonnage,
            NetTonnage = spec?.NetTonnage,
            PassengerCapacity = spec?.PassengerCapacity,
            CrewCapacity = spec?.CrewCapacity,
            ImoNumber = v.Vessel.ImoNumber,
            MmsiNumber = v.Vessel.MmsiNumber,
            CallSign = v.Vessel.CallSign,
            HomePort = v.Vessel.HomeMarinaName,
            Latitude = currentLocation?.Latitude != null ? (double?)decimal.ToDouble(currentLocation.Latitude.Value) : null,
            Longitude = currentLocation?.Longitude != null ? (double?)decimal.ToDouble(currentLocation.Longitude.Value) : null,
            LastPositionDate = currentLocation?.CapturedAt,
            OperationalStatus = null, // not yet on VesselDto, populated when vessel module exposes it
            Status = (int)v.Vessel.Status,
            IsArchived = v.Vessel.IsArchived,
            CreateDate = v.Vessel.CreateDate,
            PrimaryEngine = primaryEngine != null ? new VesselEngineBffDto
            {
                EngineType = primaryEngine.EngineTypeCode,
                EngineModel = primaryEngine.Model,
                Brand = primaryEngine.Brand,
                PropulsionType = primaryEngine.PropulsionType,
                FuelType = primaryEngine.FuelTypeCode,
                HorsePower = primaryEngine.HorsePower,
                EnginePowerKw = primaryEngine.EnginePowerKw,
                FuelCapacityL = primaryEngine.FuelCapacityL,
                MaxSpeedKnots = primaryEngine.MaxSpeedKnots,
                CruisingSpeedKnots = primaryEngine.CruisingSpeedKnots,
                RangeNm = primaryEngine.RangeNm,
                IsPrimary = primaryEngine.IsPrimary
            } : null,
            CargoDryKits = new(), // CargoDry module not yet integrated
            DocumentSummaries = v.Documents?.Select(d => new DocumentSummaryBffDto
            {
                Id = d.Id,
                DocumentType = d.DocumentTypeCode,
                ExpiryDate = d.ExpiresAt,
                DaysUntilExpiry = d.ExpiresAt.HasValue ? (int?)(d.ExpiresAt.Value - DateTime.UtcNow).TotalDays : null,
                DocumentStatus = d.Status.ToString()
            }).ToList() ?? new()
        };

        if (serviceHistoryTask.IsCompletedSuccessfully && serviceHistoryTask.Result?.Body?.Items != null)
        {
            response.Vessel.ServiceHistory = serviceHistoryTask.Result.Body.Items.Select(s => new ServiceHistoryItemBffDto
            {
                Id = s.Id,
                Date = s.RequestedStartDate ?? s.CreatedAt,
                ServiceType = s.ServiceTypeCode ?? s.ServiceCategoryCode,
                Provider = null, // ProviderName requires Identity/Profile integration — documented as gap
                Location = s.LocationMarinaName,
                Notes = s.OwnerNotes ?? s.Title,
                Status = s.Status.ToString().ToLowerInvariant()
            }).ToList();
        }
        else
        {
            response.Warnings.Add(AdminBffWarning.CallFailed("ServiceRequest.History", "Could not retrieve service history."));
        }

        return response;
    }
}
