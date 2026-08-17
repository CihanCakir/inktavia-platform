using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Bff.AdminPanel.Application.Dashboard.Dto;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Dashboard.Query;

[DocumentationInfo("Get admin dashboard charts query handler",
    "Aggregates the three dashboard chart series (SR volume C2, vessel fleet status C3, monthly revenue/commission C1) " +
    "from downstream modules. Each call is best-effort: on failure that series is left empty and an AdminBffWarning is " +
    "appended (never a 500).")]
public sealed class GetDashboardChartsBffQueryHandler
    : AizenQueryHandler<GetDashboardChartsBffQuery, AdminDashboardChartsResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly IVesselRemoteCall _vessel;
    private readonly IPaymentRemoteCall _payment;

    public GetDashboardChartsBffQueryHandler(
        IServiceRequestRemoteCall serviceRequest,
        IVesselRemoteCall vessel,
        IPaymentRemoteCall payment)
    {
        _serviceRequest = serviceRequest;
        _vessel = vessel;
        _payment = payment;
    }

    public override async Task<AdminDashboardChartsResponse?> Handle(
        GetDashboardChartsBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminDashboardChartsResponse();

        // C2 — service-request volume by status.
        try
        {
            var sr = await _serviceRequest.GetAdminServiceRequestStatusBreakdown();
            response.ServiceRequestVolume = sr.Body ?? new List<StatusCountDto>();
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
        }

        // C3 — vessel fleet status.
        try
        {
            var vessels = await _vessel.GetVesselStatusCounts();
            response.VesselFleetStatus = vessels.Body ?? new List<StatusCountDto>();
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));
        }

        // C1 — monthly revenue/commission (last 12 months).
        try
        {
            var revenue = await _payment.GetMonthlyRevenueCommissionReportAsync(12, cancellationToken);
            response.Revenue = revenue ?? new List<MonthlyRevenueCommissionDto>();
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Payment"));
        }

        return response;
    }
}
