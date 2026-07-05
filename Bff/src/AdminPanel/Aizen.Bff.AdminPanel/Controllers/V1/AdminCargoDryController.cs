using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.CancelCargoDryRenewalPreparation;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.CompleteCargoDryRenewal;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.DispatchCargoDryRenewalNotification;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.PrepareCargoDryKitRenewal;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.PrepareCargoDryRenewalInvoice;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.PrepareCargoDryRenewalNotification;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryRenewalCandidatesBff;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryRenewalPreparationDetailBff;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryRenewalPreparationsPagedBff;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryKitLifecycleHistoryBff;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryKitLifecycleEventsPagedBff;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryOperationalAlertsBff;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryOperationalOverviewBff;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.RunCargoDryMonthlySettlementAutomation;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySettlementAutomationPreview;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySettlementAutomationRunDetail;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySettlementAutomationRunsPaged;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.ActivateConsignmentAgreement;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.ApproveCargoDrySettlementPayout;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.CompleteCargoDrySettlementPayout;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.FailCargoDrySettlementPayout;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.MarkCargoDrySettlementPayoutProcessing;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.PrepareCargoDrySettlementInvoice;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryCommercialRuleResolutionPreview;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySalesAttributionRuleResolutionPreview;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySettlementInvoicePreparationPreview;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySettlementPayoutExecutionPreview;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.PrepareCargoDrySettlementPayment;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.ResolveCargoDrySalesAttributionFinancials;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.ResolveMonthlySellThroughSettlement;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySalesAttributionDetail;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySalesAttributionsPaged;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySettlementPaymentPreparationPreview;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySellThroughSettlementDetail;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySellThroughSettlementsPaged;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.AdjustProviderInventory;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.AllocateBatchToProvider;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.CreateCargoDryProduct;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.CreateConsignmentAgreement;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.ExtendKit;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.GenerateCargoDryBatch;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.RenewKit;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.RevokeBatch;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.RevokeKit;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.SuspendConsignmentAgreement;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.TerminateConsignmentAgreement;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.TransferKit;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.UpdateCargoDryProduct;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.UpdateConsignmentAgreement;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetActiveConsignmentAgreementForProvider;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryAllocationPreview;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryInventoryDetail;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryInventoryList;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryInventoryMovements;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetConsignmentAgreementByCode;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetConsignmentAgreementById;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetConsignmentAgreementsPaged;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.ExportCargoDryBatches;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.ExportCargoDryKits;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.ExportCargoDryUsageReport;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryAnalytics;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryBatchByCode;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryBatchList;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryKitList;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryKitDetailBff;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.LookupCargoDryKitAdminBff;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryProductDetail;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryProducts;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryStats;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryStatsComparison;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryUsageReport;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryWarehouseOptions;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/cargodry")]
[Tags("Admin Panel - CargoDry")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class AdminCargoDryController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminCargoDryController(
        IHttpContextAccessor httpContextAccessor,
        IAizenCQRSProcessor  cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    // ── Kits ─────────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/kits</summary>
    [HttpGet("kits")]
    [ProducesResponseType(typeof(CargoDryKitListBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitListBffDto>> GetKits(
        [FromQuery] string? status      = null,
        [FromQuery] string? search      = null,
        [FromQuery] long?   vesselId    = null,
        [FromQuery] long?   ownerUserId = null,
        [FromQuery] string? batchCode   = null,
        [FromQuery] int     page        = 1,
        [FromQuery] int     pageSize    = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryKitListBffQuery
            {
                Status      = status,
                Search      = search,
                VesselId    = vesselId,
                OwnerUserId = ownerUserId,
                BatchCode   = batchCode,
                Page        = page,
                PageSize    = pageSize,
            }, ct);

        return SetResponse(result?.KitList);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/kits/lookup?q={query} — exact kit lookup by id, kit code, or serial</summary>
    [HttpGet("kits/lookup")]
    [ProducesResponseType(typeof(LookupCargoDryKitAdminBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupCargoDryKitAdminBffResponse>> LookupKit(
        [FromQuery] string q,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new LookupCargoDryKitAdminBffQuery { Query = q }, ct);
        return SetResponse(result);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/kits/{id} — full admin kit detail</summary>
    [HttpGet("kits/{id:long}")]
    [ProducesResponseType(typeof(GetCargoDryKitDetailBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetCargoDryKitDetailBffResponse>> GetKitDetail(
        long id,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryKitDetailBffQuery { KitId = id }, ct);
        return SetResponse(result);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/kits/export — CSV download</summary>
    [HttpGet("kits/export")]
    public async Task<IActionResult> ExportKits(
        [FromQuery] string? status    = null,
        [FromQuery] string? search    = null,
        [FromQuery] long?   vesselId  = null,
        [FromQuery] string? batchCode = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new ExportCargoDryKitsBffQuery
            {
                Status    = status,
                Search    = search,
                VesselId  = vesselId,
                BatchCode = batchCode,
            }, ct);

        if (result is null) return BadRequest();
        return File(result.Bytes, result.ContentType, result.FileName);
    }

    // ── Kit Actions ───────────────────────────────────────────────────────────

    /// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/transfer</summary>
    [HttpPost("kits/{id:long}/transfer")]
    [ProducesResponseType(typeof(TransferKitBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<TransferKitBffResponse>> TransferKit(
        long id, [FromBody] TransferKitBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new TransferCargoDryKitBffCommand
            {
                KitId       = id,
                NewUserId   = body.NewUserId,
                NewVesselId = body.NewVesselId,
            }, ct);

        return SetResponse(result?.Result);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/revoke</summary>
    [HttpPost("kits/{id:long}/revoke")]
    [ProducesResponseType(typeof(RevokeKitBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RevokeKitBffResponse>> RevokeKit(
        long id, [FromBody] RevokeKitBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RevokeKitBffCommand { KitId = id, Reason = body.Reason }, ct);

        return SetResponse(result?.Result);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/renew</summary>
    [HttpPost("kits/{id:long}/renew")]
    [ProducesResponseType(typeof(CargoDryKitBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitBffDto>> RenewKit(
        long id, [FromBody] RenewKitBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RenewKitBffCommand
            {
                KitId      = id,
                AddedDays  = body.AddedDays,
                PaymentRef = body.PaymentRef,
            }, ct);

        return SetResponse(result?.Kit);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/extend</summary>
    [HttpPost("kits/{id:long}/extend")]
    [ProducesResponseType(typeof(CargoDryKitBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitBffDto>> ExtendKit(
        long id, [FromBody] ExtendKitBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ExtendKitBffCommand { KitId = id, AddedDays = body.AddedDays }, ct);

        return SetResponse(result?.Kit);
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/stats</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CargoDryStatsBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryStatsBffDto>> GetStats(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryStatsBffQuery(), ct);
        return SetResponse(result?.Stats);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/stats/comparison — prior-period KPI deltas</summary>
    [HttpGet("stats/comparison")]
    [ProducesResponseType(typeof(CargoDryStatsComparisonBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryStatsComparisonBffDto>> GetStatsComparison(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryStatsComparisonBffQuery(), ct);
        return SetResponse(result?.Comparison);
    }

    // ── Analytics ─────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/analytics</summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(CargoDryAnalyticsBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryAnalyticsBffDto>> GetAnalytics(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryAnalyticsBffQuery(), ct);
        return SetResponse(result?.Analytics);
    }

    // ── Products ──────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/products</summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(List<CargoDryProductBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CargoDryProductBffDto>>> GetProducts(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryProductsBffQuery(), ct);
        return SetResponse(result?.Products);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/products/{productCode}</summary>
    [HttpGet("products/{productCode}")]
    [ProducesResponseType(typeof(CargoDryProductBffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductDetail(string productCode, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryProductDetailBffQuery { ProductCode = productCode }, ct);

        if (result?.Product is null) return NotFound();
        return Ok(SetResponse(result.Product));
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/products</summary>
    [HttpPost("products")]
    [ProducesResponseType(typeof(CargoDryProductBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryProductBffDto>> CreateProduct(
        [FromBody] CreateCargoDryProductBffCommand command, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result?.Product);
    }

    /// <summary>PUT /api/v1/admin-panel/cargodry/products/{productCode}</summary>
    [HttpPut("products/{productCode}")]
    [ProducesResponseType(typeof(CargoDryProductBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryProductBffDto>> UpdateProduct(
        string productCode,
        [FromBody] UpdateProductBody body,
        CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new UpdateCargoDryProductBffCommand
            {
                ProductCode    = productCode,
                Name           = body.Name,
                Description    = body.Description,
                ValidityDays   = body.ValidityDays,
                RetailPrice    = body.RetailPrice,
                CurrencyCode   = body.CurrencyCode,
                HasSmartDevice = body.HasSmartDevice,
                DeviceType     = body.DeviceType,
                IsActive       = body.IsActive,
            }, ct);

        return SetResponse(result?.Product);
    }

    // ── Batches ───────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/batches</summary>
    [HttpGet("batches")]
    [ProducesResponseType(typeof(CargoDryBatchListBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryBatchListBffDto>> GetBatches(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryBatchListBffQuery { Page = page, PageSize = pageSize }, ct);

        return SetResponse(result?.BatchList);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/batches/export — CSV download</summary>
    [HttpGet("batches/export")]
    public async Task<IActionResult> ExportBatches(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new ExportCargoDryBatchesBffQuery(), ct);

        if (result is null) return BadRequest();
        return File(result.Bytes, result.ContentType, result.FileName);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/batches/{batchCode}</summary>
    [HttpGet("batches/{batchCode}")]
    [ProducesResponseType(typeof(CargoDryBatchBffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchByCode(string batchCode, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryBatchByCodeBffQuery { BatchCode = batchCode }, ct);

        if (result?.Batch is null) return NotFound();
        return Ok(SetResponse(result.Batch));
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/batches/{batchCode}/revoke</summary>
    [HttpPost("batches/{batchCode}/revoke")]
    [ProducesResponseType(typeof(RevokeBatchBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RevokeBatchBffResponse>> RevokeBatch(
        string batchCode, [FromBody] RevokeBatchBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RevokeCargoDryBatchBffCommand { BatchCode = batchCode, Reason = body.Reason }, ct);

        return SetResponse(result?.Result);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/batches/generate</summary>
    [HttpPost("batches/generate")]
    [ProducesResponseType(typeof(GenerateBatchBffResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GenerateBatchBffResultDto>> GenerateBatch(
        [FromBody] GenerateCargoDryBatchBffCommand command, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result?.Result);
    }

    // ── Warehouses ────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/warehouses</summary>
    [HttpGet("warehouses")]
    [ProducesResponseType(typeof(List<CargoDryWarehouseOptionBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CargoDryWarehouseOptionBffDto>>> GetWarehouses(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetCargoDryWarehouseOptionsBffQuery(), ct);
        return SetResponse(result?.Warehouses);
    }

    // ── Reports ───────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/reports/usage</summary>
    [HttpGet("reports/usage")]
    [ProducesResponseType(typeof(CargoDryKitUsageReportBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitUsageReportBffDto>> GetUsageReport(
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo   = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryUsageReportBffQuery { DateFrom = dateFrom, DateTo = dateTo }, ct);

        return SetResponse(result?.Report);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/reports/usage/export</summary>
    [HttpGet("reports/usage/export")]
    public async Task<IActionResult> ExportUsageReport(
        [FromQuery] string          format   = "csv",
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo   = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new ExportCargoDryUsageReportBffQuery
            {
                Format   = format,
                DateFrom = dateFrom,
                DateTo   = dateTo,
            }, ct);

        if (result is null) return BadRequest();

        return File(result.Bytes, result.ContentType, result.FileName);
    }

    // ── Consignment Agreements ────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/consignment/agreements</summary>
    [HttpGet("consignment/agreements")]
    [ProducesResponseType(typeof(ConsignmentAgreementPagedBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ConsignmentAgreementPagedBffDto>> GetConsignmentAgreementsPaged(
        [FromQuery] long?     providerProfileId = null,
        [FromQuery] string?   productCode       = null,
        [FromQuery] int?      status            = null,
        [FromQuery] DateTime? dateFrom          = null,
        [FromQuery] DateTime? dateTo            = null,
        [FromQuery] string?   search            = null,
        [FromQuery] int       page              = 1,
        [FromQuery] int       pageSize          = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetConsignmentAgreementsPagedBffQuery
            {
                ProviderProfileId = providerProfileId,
                ProductCode       = productCode,
                Status            = status,
                DateFrom          = dateFrom,
                DateTo            = dateTo,
                Search            = search,
                Page              = page,
                PageSize          = pageSize,
            }, ct);

        return SetResponse(result?.PagedResult);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/consignment/agreements/{id}</summary>
    [HttpGet("consignment/agreements/{id:long}")]
    [ProducesResponseType(typeof(ConsignmentAgreementBffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsignmentAgreementById(long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetConsignmentAgreementByIdBffQuery { Id = id }, ct);

        if (result?.Agreement is null) return NotFound();
        return Ok(SetResponse(result.Agreement));
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/consignment/agreements/code/{code}</summary>
    [HttpGet("consignment/agreements/code/{code}")]
    [ProducesResponseType(typeof(ConsignmentAgreementBffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsignmentAgreementByCode(string code, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetConsignmentAgreementByCodeBffQuery { AgreementCode = code }, ct);

        if (result?.Agreement is null) return NotFound();
        return Ok(SetResponse(result.Agreement));
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/consignment/agreements/provider/{providerProfileId}/active?productCode=X</summary>
    [HttpGet("consignment/agreements/provider/{providerProfileId:long}/active")]
    [ProducesResponseType(typeof(ConsignmentAgreementBffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveConsignmentAgreementForProvider(
        long providerProfileId,
        [FromQuery] string productCode,
        CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetActiveConsignmentAgreementForProviderBffQuery
            {
                ProviderProfileId = providerProfileId,
                ProductCode       = productCode,
            }, ct);

        if (result?.Agreement is null) return NotFound();
        return Ok(SetResponse(result.Agreement));
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/consignment/agreements</summary>
    [HttpPost("consignment/agreements")]
    [ProducesResponseType(typeof(ConsignmentAgreementBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ConsignmentAgreementBffDto>> CreateConsignmentAgreement(
        [FromBody] CreateConsignmentAgreementBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new CreateConsignmentAgreementBffCommand
            {
                AgreementCode           = body.AgreementCode,
                ProviderProfileId       = body.ProviderProfileId,
                ProductCode             = body.ProductCode,
                ConsignmentRate         = body.ConsignmentRate,
                MinimumSettlementAmount = body.MinimumSettlementAmount,
                CurrencyCode            = body.CurrencyCode,
                MaxKitCount             = body.MaxKitCount,
                StartDateUtc            = body.StartDateUtc,
                EndDateUtc              = body.EndDateUtc,
                TermsDocumentRef        = body.TermsDocumentRef,
                Notes                   = body.Notes,
            }, ct);

        return SetResponse(result?.Agreement);
    }

    /// <summary>PUT /api/v1/admin-panel/cargodry/consignment/agreements/{id}</summary>
    [HttpPut("consignment/agreements/{id:long}")]
    [ProducesResponseType(typeof(ConsignmentAgreementBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ConsignmentAgreementBffDto>> UpdateConsignmentAgreement(
        long id, [FromBody] UpdateConsignmentAgreementBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new UpdateConsignmentAgreementBffCommand
            {
                Id                      = id,
                ConsignmentRate         = body.ConsignmentRate,
                MinimumSettlementAmount = body.MinimumSettlementAmount,
                CurrencyCode            = body.CurrencyCode,
                MaxKitCount             = body.MaxKitCount,
                StartDateUtc            = body.StartDateUtc,
                EndDateUtc              = body.EndDateUtc,
                TermsDocumentRef        = body.TermsDocumentRef,
                Notes                   = body.Notes,
            }, ct);

        return SetResponse(result?.Agreement);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/consignment/agreements/{id}/activate</summary>
    [HttpPost("consignment/agreements/{id:long}/activate")]
    [ProducesResponseType(typeof(ConsignmentAgreementBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ConsignmentAgreementBffDto>> ActivateConsignmentAgreement(
        long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ActivateConsignmentAgreementBffCommand { Id = id }, ct);

        return SetResponse(result?.Agreement);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/consignment/agreements/{id}/suspend</summary>
    [HttpPost("consignment/agreements/{id:long}/suspend")]
    [ProducesResponseType(typeof(ConsignmentAgreementBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ConsignmentAgreementBffDto>> SuspendConsignmentAgreement(
        long id, [FromBody] AgreementReasonBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new SuspendConsignmentAgreementBffCommand { Id = id, Reason = body.Reason }, ct);

        return SetResponse(result?.Agreement);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/consignment/agreements/{id}/terminate</summary>
    [HttpPost("consignment/agreements/{id:long}/terminate")]
    [ProducesResponseType(typeof(ConsignmentAgreementBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ConsignmentAgreementBffDto>> TerminateConsignmentAgreement(
        long id, [FromBody] AgreementReasonBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new TerminateConsignmentAgreementBffCommand { Id = id, Reason = body.Reason }, ct);

        return SetResponse(result?.Agreement);
    }

    // ── Provider Inventory ────────────────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/inventory</summary>
    [HttpGet("inventory")]
    [ProducesResponseType(typeof(CargoDryProviderInventoryPagedBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryProviderInventoryPagedBffDto>> GetInventoryList(
        [FromQuery] long?   providerProfileId = null,
        [FromQuery] string? productCode       = null,
        [FromQuery] int?    commercialModel   = null,
        [FromQuery] int?    salesChannel      = null,
        [FromQuery] bool?   hasAvailableStock = null,
        [FromQuery] string? search            = null,
        [FromQuery] int     page              = 1,
        [FromQuery] int     pageSize          = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryInventoryListBffQuery
            {
                ProviderProfileId = providerProfileId,
                ProductCode       = productCode,
                CommercialModel   = commercialModel,
                SalesChannel      = salesChannel,
                HasAvailableStock = hasAvailableStock,
                Search            = search,
                Page              = page,
                PageSize          = pageSize,
            }, ct);

        return SetResponse(result?.PagedResult);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/inventory/provider/{providerProfileId}</summary>
    [HttpGet("inventory/provider/{providerProfileId:long}")]
    [ProducesResponseType(typeof(CargoDryProviderInventoryDetailBffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInventoryDetail(long providerProfileId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryInventoryDetailBffQuery { ProviderProfileId = providerProfileId }, ct);

        if (result?.Detail is null) return NotFound();
        return Ok(SetResponse(result.Detail));
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/inventory/movements</summary>
    [HttpGet("inventory/movements")]
    [ProducesResponseType(typeof(CargoDryInventoryMovementPagedBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryInventoryMovementPagedBffDto>> GetInventoryMovements(
        [FromQuery] long?     providerProfileId = null,
        [FromQuery] string?   productCode       = null,
        [FromQuery] string?   batchCode         = null,
        [FromQuery] int?      movementType      = null,
        [FromQuery] DateTime? dateFrom          = null,
        [FromQuery] DateTime? dateTo            = null,
        [FromQuery] int       page              = 1,
        [FromQuery] int       pageSize          = 50,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryInventoryMovementsBffQuery
            {
                ProviderProfileId = providerProfileId,
                ProductCode       = productCode,
                BatchCode         = batchCode,
                MovementType      = movementType,
                DateFrom          = dateFrom,
                DateTo            = dateTo,
                Page              = page,
                PageSize          = pageSize,
            }, ct);

        return SetResponse(result?.PagedResult);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/inventory/preview?batchCode=X&amp;providerProfileId=Y&amp;commercialModel=Z</summary>
    [HttpGet("inventory/preview")]
    [ProducesResponseType(typeof(BatchAllocationPreviewBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<BatchAllocationPreviewBffDto>> GetAllocationPreview(
        [FromQuery] string batchCode,
        [FromQuery] long   providerProfileId,
        [FromQuery] int    commercialModel,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryAllocationPreviewBffQuery
            {
                BatchCode         = batchCode,
                ProviderProfileId = providerProfileId,
                CommercialModel   = commercialModel,
            }, ct);

        return SetResponse(result?.Preview);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/inventory/allocate</summary>
    [HttpPost("inventory/allocate")]
    [ProducesResponseType(typeof(AllocateBatchToProviderBffResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AllocateBatchToProviderBffResultDto>> AllocateBatchToProvider(
        [FromBody] AllocateBatchToProviderBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new AllocateBatchToProviderBffCommand
            {
                BatchCode              = body.BatchCode,
                ProviderProfileId      = body.ProviderProfileId,
                CommercialModel        = body.CommercialModel,
                SalesChannel           = body.SalesChannel,
                ConsignmentAgreementId = body.ConsignmentAgreementId,
                WarehouseId            = body.WarehouseId,
                Note                   = body.Note,
            }, ct);

        return SetResponse(result?.Result);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/inventory/adjust</summary>
    [HttpPost("inventory/adjust")]
    [ProducesResponseType(typeof(CargoDryProviderInventoryBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryProviderInventoryBffDto>> AdjustProviderInventory(
        [FromBody] AdjustInventoryBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new AdjustProviderInventoryBffCommand
            {
                ProviderProfileId  = body.ProviderProfileId,
                ProductCode        = body.ProductCode,
                BatchCode          = body.BatchCode,
                AdjustmentQuantity = body.AdjustmentQuantity,
                Reason             = body.Reason,
            }, ct);

        return SetResponse(result?.UpdatedInventory);
    }

    // ── Commercial: Sales Attributions ───────────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/commercial/sales-attributions</summary>
    [HttpGet("commercial/sales-attributions")]
    [ProducesResponseType(typeof(CargoDrySalesAttributionPagedBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySalesAttributionPagedBffDto>> GetSalesAttributionsPaged(
        [FromQuery] long?     providerProfileId       = null,
        [FromQuery] string?   productCode             = null,
        [FromQuery] string?   batchCode               = null,
        [FromQuery] int?      salesChannel            = null,
        [FromQuery] int?      commercialModel         = null,
        [FromQuery] int?      status                  = null,
        [FromQuery] long?     sellThroughSettlementId = null,
        [FromQuery] DateTime? dateFrom                = null,
        [FromQuery] DateTime? dateTo                  = null,
        [FromQuery] string?   search                  = null,
        [FromQuery] int       page                    = 1,
        [FromQuery] int       pageSize                = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDrySalesAttributionsPagedBffQuery
            {
                ProviderProfileId       = providerProfileId,
                ProductCode             = productCode,
                BatchCode               = batchCode,
                SalesChannel            = salesChannel,
                CommercialModel         = commercialModel,
                Status                  = status,
                SellThroughSettlementId = sellThroughSettlementId,
                DateFrom                = dateFrom,
                DateTo                  = dateTo,
                Search                  = search,
                Page                    = page,
                PageSize                = pageSize,
            }, ct);

        return SetResponse(result?.PagedResult);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/commercial/sales-attributions/{id}</summary>
    [HttpGet("commercial/sales-attributions/{id:long}")]
    [ProducesResponseType(typeof(CargoDrySalesAttributionBffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<CargoDrySalesAttributionBffDto>> GetSalesAttributionDetail(
        long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDrySalesAttributionDetailBffQuery { Id = id }, ct);

        return SetResponse(result?.Detail);
    }

    // ── Commercial: Sell-Through Settlements ──────────────────────────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/commercial/settlements</summary>
    [HttpGet("commercial/settlements")]
    [ProducesResponseType(typeof(CargoDrySellThroughSettlementPagedBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySellThroughSettlementPagedBffDto>> GetSellThroughSettlementsPaged(
        [FromQuery] long?     providerProfileId      = null,
        [FromQuery] long?     consignmentAgreementId = null,
        [FromQuery] string?   productCode            = null,
        [FromQuery] int?      status                 = null,
        [FromQuery] DateTime? periodFrom             = null,
        [FromQuery] DateTime? periodTo               = null,
        [FromQuery] string?   search                 = null,
        [FromQuery] int       page                   = 1,
        [FromQuery] int       pageSize               = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDrySellThroughSettlementsPagedBffQuery
            {
                ProviderProfileId      = providerProfileId,
                ConsignmentAgreementId = consignmentAgreementId,
                ProductCode            = productCode,
                Status                 = status,
                PeriodFrom             = periodFrom,
                PeriodTo               = periodTo,
                Search                 = search,
                Page                   = page,
                PageSize               = pageSize,
            }, ct);

        return SetResponse(result?.PagedResult);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/commercial/settlements/{id}</summary>
    [HttpGet("commercial/settlements/{id:long}")]
    [ProducesResponseType(typeof(CargoDrySellThroughSettlementBffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<CargoDrySellThroughSettlementBffDto>> GetSellThroughSettlementDetail(
        long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDrySellThroughSettlementDetailBffQuery { Id = id }, ct);

        return SetResponse(result?.Detail);
    }

    // ── Phase 5: Commercial Rule Resolution Preview ───────────────────────────

    /// <summary>
    /// GET /api/v1/admin-panel/cargodry/commercial/rules/resolve-preview
    /// Runs the 7-tier CargoDry commercial rule resolver in read-only mode against the supplied inputs.
    /// Never throws for business ineligibility — returns CanResolve=false + BlockingReasons instead.
    /// Safe to call at any time. Phase 5 (July 2026).
    /// </summary>
    [HttpGet("commercial/rules/resolve-preview")]
    [ProducesResponseType(typeof(CargoDryCommercialRuleResolutionBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryCommercialRuleResolutionBffDto>> GetRuleResolutionPreview(
        [FromQuery] string   productCode            = "",
        [FromQuery] int      salesChannel           = 0,
        [FromQuery] int      commercialModel        = 0,
        [FromQuery] string   currencyCode           = "TRY",
        [FromQuery] long?    providerProfileId      = null,
        [FromQuery] decimal? salePrice              = null,
        [FromQuery] long?    consignmentAgreementId = null,
        [FromQuery] decimal? adminOverrideRate      = null,
        [FromQuery] DateTime? effectiveAtUtc        = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryCommercialRuleResolutionPreviewBffQuery
            {
                ProductCode            = productCode,
                SalesChannel           = salesChannel,
                CommercialModel        = commercialModel,
                CurrencyCode           = currencyCode,
                ProviderProfileId      = providerProfileId,
                SalePrice              = salePrice,
                ConsignmentAgreementId = consignmentAgreementId,
                AdminOverrideRate      = adminOverrideRate,
                EffectiveAtUtc         = effectiveAtUtc,
            }, ct);

        return SetResponse(result?.Resolution);
    }

    /// <summary>
    /// GET /api/v1/admin-panel/cargodry/commercial/sales-attributions/{id}/rule-resolution-preview
    /// Loads the attribution record and runs the resolver against its context.
    /// Optional query params override salePrice, currencyCode, or inject an adminOverrideRate.
    /// Returns the current attribution DTO plus the full resolution result.
    /// Never throws for business ineligibility. Phase 5 (July 2026).
    /// </summary>
    [HttpGet("commercial/sales-attributions/{id:long}/rule-resolution-preview")]
    [ProducesResponseType(typeof(CargoDrySalesAttributionRuleResolutionPreviewBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySalesAttributionRuleResolutionPreviewBffDto>> GetAttributionRuleResolutionPreview(
        long id,
        [FromQuery] decimal? salePrice        = null,
        [FromQuery] string?  currencyCode      = null,
        [FromQuery] decimal? adminOverrideRate = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDrySalesAttributionRuleResolutionPreviewBffQuery
            {
                SalesAttributionId = id,
                SalePrice          = salePrice,
                CurrencyCode       = currencyCode,
                AdminOverrideRate  = adminOverrideRate,
            }, ct);

        return SetResponse(result?.Preview);
    }

    // ── Phase 4A: Financial Resolution ───────────────────────────────────────

    /// <summary>POST /api/v1/admin-panel/cargodry/commercial/sales-attributions/{id}/resolve-financials</summary>
    [HttpPost("commercial/sales-attributions/{id:long}/resolve-financials")]
    [ProducesResponseType(typeof(CargoDrySalesAttributionBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySalesAttributionBffDto>> ResolveAttributionFinancials(
        long id, [FromBody] ResolveAttributionFinancialsBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ResolveCargoDrySalesAttributionFinancialsBffCommand
            {
                SalesAttributionId    = id,
                SalePrice             = body.SalePrice,
                CurrencyCode          = body.CurrencyCode,
                CommissionRateOverride = body.CommissionRateOverride,
                ResolvedByUserId      = body.ResolvedByUserId,
                ResolutionNote        = body.ResolutionNote,
            }, ct);

        return SetResponse(result?.Attribution);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/commercial/settlements/{id}/resolve-monthly</summary>
    [HttpPost("commercial/settlements/{id:long}/resolve-monthly")]
    [ProducesResponseType(typeof(CargoDrySellThroughSettlementBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySellThroughSettlementBffDto>> ResolveMonthlySettlement(
        long id, [FromBody] ResolveMonthlySettlementBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ResolveMonthlySellThroughSettlementBffCommand
            {
                SettlementId     = id,
                ResolvedByUserId = body.ResolvedByUserId,
                ResolutionNote   = body.ResolutionNote,
            }, ct);

        return SetResponse(result?.Settlement);
    }

    // ── Phase 4C: Settlement Invoice Preparation ─────────────────────────────

    /// <summary>
    /// GET /api/v1/admin-panel/cargodry/commercial/settlements/{id}/invoice-preparation-preview
    /// Returns an eligibility preview for invoice preparation of the given Scheduled settlement.
    /// Never throws for business ineligibility — returns CanPrepare=false + BlockingReasons instead.
    /// </summary>
    [HttpGet("commercial/settlements/{id:long}/invoice-preparation-preview")]
    [ProducesResponseType(typeof(CargoDrySettlementInvoicePreparationPreviewBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySettlementInvoicePreparationPreviewBffDto>> GetSettlementInvoicePreparationPreview(
        long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDrySettlementInvoicePreparationPreviewBffQuery { SettlementId = id }, ct);

        return SetResponse(result?.Preview);
    }

    /// <summary>
    /// POST /api/v1/admin-panel/cargodry/commercial/settlements/{id}/prepare-invoice
    /// Prepares a Draft ProviderSettlementStatement invoice for the given Scheduled settlement.
    /// Settlement status remains Scheduled. Idempotent — returns existing invoice if already prepared.
    /// </summary>
    [HttpPost("commercial/settlements/{id:long}/prepare-invoice")]
    [ProducesResponseType(typeof(PrepareCargoDrySettlementInvoiceBffCommandResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PrepareCargoDrySettlementInvoiceBffCommandResponse>> PrepareSettlementInvoice(
        long id, [FromBody] PrepareSettlementInvoiceBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new PrepareCargoDrySettlementInvoiceBffCommand
            {
                SettlementId     = id,
                PreparedByUserId = body.PreparedByUserId,
                PreparationNote  = body.PreparationNote,
            }, ct);

        return SetResponse(result);
    }

    // ── Phase 4B: Settlement Payment Preparation ─────────────────────────────

    /// <summary>
    /// GET /api/v1/admin-panel/cargodry/commercial/settlements/{id}/payment-preparation-preview
    /// Returns an eligibility preview for payment preparation of the given settlement.
    /// Never throws for business ineligibility — returns CanPrepare=false + BlockingReasons instead.
    /// </summary>
    [HttpGet("commercial/settlements/{id:long}/payment-preparation-preview")]
    [ProducesResponseType(typeof(CargoDrySettlementPaymentPreparationPreviewBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySettlementPaymentPreparationPreviewBffDto>> GetSettlementPaymentPreparationPreview(
        long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDrySettlementPaymentPreparationPreviewBffQuery { SettlementId = id }, ct);

        return SetResponse(result?.Preview);
    }

    /// <summary>
    /// POST /api/v1/admin-panel/cargodry/commercial/settlements/{id}/prepare-payment
    /// Prepares a PayoutRecord in the Payment module for the given ReadyForSettlement settlement
    /// and transitions the settlement to Scheduled status.
    /// Idempotent — safe to call multiple times; returns existing payout record if already prepared.
    /// </summary>
    [HttpPost("commercial/settlements/{id:long}/prepare-payment")]
    [ProducesResponseType(typeof(PrepareCargoDrySettlementPaymentBffCommandResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<PrepareCargoDrySettlementPaymentBffCommandResponse>> PrepareSettlementPayment(
        long id, [FromBody] PrepareSettlementPaymentBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new PrepareCargoDrySettlementPaymentBffCommand
            {
                SettlementId     = id,
                PreparedByUserId = body.PreparedByUserId,
                PreparationNote  = body.PreparationNote,
            }, ct);

        return SetResponse(result);
    }

    // ── Phase 4D: Provider Payout Lifecycle & Settlement Closure ─────────────

    /// <summary>
    /// GET /api/v1/admin-panel/cargodry/commercial/settlements/{id}/payout-execution-preview
    /// Returns a live eligibility preview for the payout lifecycle of the given settlement.
    /// Shows prerequisite flags (PaymentPrepared, InvoicePrepared), current payout state,
    /// and eligibility flags (CanApprovePayout, CanMarkProcessing, CanCompletePayout, CanFailPayout).
    /// Never throws for business ineligibility — returns BlockingReasons instead.
    /// </summary>
    [HttpGet("commercial/settlements/{id:long}/payout-execution-preview")]
    [ProducesResponseType(typeof(CargoDrySettlementPayoutExecutionPreviewBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySettlementPayoutExecutionPreviewBffDto>> GetSettlementPayoutExecutionPreview(
        long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDrySettlementPayoutExecutionPreviewBffQuery { SettlementId = id }, ct);

        return SetResponse(result?.Preview);
    }

    /// <summary>
    /// POST /api/v1/admin-panel/cargodry/commercial/settlements/{id}/approve-payout
    /// Approves the payout record for the given Scheduled settlement.
    /// Transitions PayoutRecord status: Pending/Processing → Approved.
    /// Settlement status remains Scheduled. Requires Phase 4B (PayoutRecord) to be complete.
    /// No Iyzico call. No automatic transfer. Phase 4D (July 2026).
    /// </summary>
    [HttpPost("commercial/settlements/{id:long}/approve-payout")]
    [ProducesResponseType(typeof(CargoDrySettlementPayoutLifecycleResponseBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySettlementPayoutLifecycleResponseBffDto>> ApproveSettlementPayout(
        long id, [FromBody] ApproveSettlementPayoutBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ApproveCargoDrySettlementPayoutBffCommand
            {
                SettlementId     = id,
                ApprovedByUserId = body.ApprovedByUserId,
                Note             = body.Note,
            }, ct);

        return SetResponse(new CargoDrySettlementPayoutLifecycleResponseBffDto
        {
            Settlement   = result?.Settlement,
            PayoutResult = result?.PayoutResult,
        });
    }

    /// <summary>
    /// POST /api/v1/admin-panel/cargodry/commercial/settlements/{id}/mark-payout-processing
    /// Marks the approved payout record as Processing (optional intermediate step).
    /// Settlement status remains Scheduled. Requires PayoutRecord in Approved state.
    /// No Iyzico call. No automatic transfer. Phase 4D (July 2026).
    /// </summary>
    [HttpPost("commercial/settlements/{id:long}/mark-payout-processing")]
    [ProducesResponseType(typeof(CargoDrySettlementPayoutLifecycleResponseBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySettlementPayoutLifecycleResponseBffDto>> MarkSettlementPayoutProcessing(
        long id, [FromBody] MarkSettlementPayoutProcessingBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new MarkCargoDrySettlementPayoutProcessingBffCommand
            {
                SettlementId      = id,
                ProcessedByUserId = body.ProcessedByUserId,
                ExternalReference = body.ExternalReference,
                Note              = body.Note,
            }, ct);

        return SetResponse(new CargoDrySettlementPayoutLifecycleResponseBffDto
        {
            Settlement   = result?.Settlement,
            PayoutResult = result?.PayoutResult,
        });
    }

    /// <summary>
    /// POST /api/v1/admin-panel/cargodry/commercial/settlements/{id}/complete-payout
    /// Records the manual payout completion and closes the settlement as Settled.
    /// THIS IS THE ONLY ENDPOINT that advances settlement status to Settled.
    /// ManualPaymentReference is required. Idempotent if already Settled.
    /// Requires Phase 4B (PayoutRecord) AND Phase 4C (InvoiceId) to be complete first.
    /// No Iyzico call. No automatic transfer. Phase 4D (July 2026).
    /// </summary>
    [HttpPost("commercial/settlements/{id:long}/complete-payout")]
    [ProducesResponseType(typeof(CargoDrySettlementPayoutLifecycleResponseBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySettlementPayoutLifecycleResponseBffDto>> CompleteSettlementPayout(
        long id, [FromBody] CompleteSettlementPayoutBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new CompleteCargoDrySettlementPayoutBffCommand
            {
                SettlementId           = id,
                CompletedByUserId      = body.CompletedByUserId,
                ManualPaymentReference = body.ManualPaymentReference,
                Note                   = body.Note,
            }, ct);

        return SetResponse(new CargoDrySettlementPayoutLifecycleResponseBffDto
        {
            Settlement       = result?.Settlement,
            PayoutResult     = result?.PayoutResult,
            AlreadyCompleted = result?.AlreadyCompleted ?? false,
        });
    }

    /// <summary>
    /// POST /api/v1/admin-panel/cargodry/commercial/settlements/{id}/fail-payout
    /// Records a payout failure on both the PayoutRecord and the settlement.
    /// Settlement status remains Scheduled — allows admin to retry after resolving the failure.
    /// FailureReason is required. Cannot fail an already-Settled settlement.
    /// No Iyzico call. No automatic transfer. Phase 4D (July 2026).
    /// </summary>
    [HttpPost("commercial/settlements/{id:long}/fail-payout")]
    [ProducesResponseType(typeof(CargoDrySettlementPayoutLifecycleResponseBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySettlementPayoutLifecycleResponseBffDto>> FailSettlementPayout(
        long id, [FromBody] FailSettlementPayoutBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new FailCargoDrySettlementPayoutBffCommand
            {
                SettlementId      = id,
                FailedByUserId    = body.FailedByUserId,
                FailureReason     = body.FailureReason,
                ExternalReference = body.ExternalReference,
                Note              = body.Note,
            }, ct);

        return SetResponse(new CargoDrySettlementPayoutLifecycleResponseBffDto
        {
            Settlement   = result?.Settlement,
            PayoutResult = result?.PayoutResult,
        });
    }

    // ── Phase 6: Settlement Automation ───────────────────────────────────────

    /// <summary>
    /// GET /api/v1/admin-panel/cargodry/commercial/settlement-automation/preview
    /// Returns a read-only prediction of what the automation run would do for the target year-month.
    /// No mutations. AutoCompletePayout is always false. Phase 6 (July 2026).
    /// </summary>
    [HttpGet("commercial/settlement-automation/preview")]
    [ProducesResponseType(typeof(CargoDrySettlementAutomationPreviewBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySettlementAutomationPreviewBffDto>> GetSettlementAutomationPreview(
        [FromQuery] int  targetYearMonth,
        [FromQuery] bool autoPreparePayment = false,
        [FromQuery] bool autoPrepareInvoice = false,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDrySettlementAutomationPreviewBffQuery
            {
                TargetYearMonth    = targetYearMonth,
                AutoPreparePayment = autoPreparePayment,
                AutoPrepareInvoice = autoPrepareInvoice,
            }, ct);

        return SetResponse(result?.Preview);
    }

    /// <summary>
    /// POST /api/v1/admin-panel/cargodry/commercial/settlement-automation/run
    /// Triggers the monthly settlement automation. Mode=1 (DryRun) by default.
    /// AutoCompletePayout is always enforced to false server-side.
    /// Phase 6 (July 2026).
    /// </summary>
    [HttpPost("commercial/settlement-automation/run")]
    [ProducesResponseType(typeof(CargoDrySettlementAutomationRunBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySettlementAutomationRunBffDto>> RunSettlementAutomation(
        [FromBody] RunSettlementAutomationBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new RunCargoDryMonthlySettlementAutomationBffCommand
            {
                TargetYearMonth    = body.TargetYearMonth,
                Mode               = body.Mode,
                AutoPreparePayment = body.AutoPreparePayment,
                AutoPrepareInvoice = body.AutoPrepareInvoice,
                TriggeredByUserId  = body.TriggeredByUserId,
                Note               = body.Note,
            }, ct);

        return SetResponse(result?.Run);
    }

    /// <summary>
    /// GET /api/v1/admin-panel/cargodry/commercial/settlement-automation/runs
    /// Paginated list of settlement automation run history. RunItems not included.
    /// Phase 6 (July 2026).
    /// </summary>
    [HttpGet("commercial/settlement-automation/runs")]
    [ProducesResponseType(typeof(CargoDrySettlementAutomationRunsPagedBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySettlementAutomationRunsPagedBffDto>> GetSettlementAutomationRuns(
        [FromQuery] int?      targetYearMonth   = null,
        [FromQuery] int?      status            = null,
        [FromQuery] int?      mode              = null,
        [FromQuery] long?     triggeredByUserId = null,
        [FromQuery] DateTime? fromUtc           = null,
        [FromQuery] DateTime? toUtc             = null,
        [FromQuery] int       skip              = 0,
        [FromQuery] int       take              = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDrySettlementAutomationRunsPagedBffQuery
            {
                TargetYearMonth   = targetYearMonth,
                Status            = status,
                Mode              = mode,
                TriggeredByUserId = triggeredByUserId,
                FromUtc           = fromUtc,
                ToUtc             = toUtc,
                Skip              = skip,
                Take              = take,
            }, ct);

        return SetResponse(result?.Result);
    }

    /// <summary>
    /// GET /api/v1/admin-panel/cargodry/commercial/settlement-automation/runs/{id}
    /// Returns a single run record with all per-settlement RunItems included.
    /// Phase 6 (July 2026).
    /// </summary>
    [HttpGet("commercial/settlement-automation/runs/{id:long}")]
    [ProducesResponseType(typeof(CargoDrySettlementAutomationRunBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDrySettlementAutomationRunBffDto>> GetSettlementAutomationRunDetail(
        long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDrySettlementAutomationRunDetailBffQuery { RunId = id }, ct);

        return SetResponse(result?.Run);
    }

    // ── Phase 9: Kit Lifecycle History & Operational Alerts ──────────────────

    /// <summary>GET /api/v1/admin-panel/cargodry/kits/{id}/history</summary>
    [HttpGet("kits/{id:long}/history")]
    [ProducesResponseType(typeof(CargoDryKitLifecycleHistoryBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitLifecycleHistoryBffResponse>> GetKitLifecycleHistory(
        long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryKitLifecycleHistoryBffQuery { KitId = id }, ct);

        return SetResponse(result?.History);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/kits/lifecycle-events</summary>
    [HttpGet("kits/lifecycle-events")]
    [ProducesResponseType(typeof(CargoDryKitLifecycleEventsPagedBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitLifecycleEventsPagedBffResponse>> GetKitLifecycleEventsPaged(
        [FromQuery] long?           kitId,
        [FromQuery] string?         kitCode,
        [FromQuery] string?         batchCode,
        [FromQuery] string?         productCode,
        [FromQuery] string?         eventType,
        [FromQuery] long?           actorUserId,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] int             page     = 1,
        [FromQuery] int             pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryKitLifecycleEventsPagedBffQuery
            {
                KitId       = kitId,
                KitCode     = kitCode,
                BatchCode   = batchCode,
                ProductCode = productCode,
                EventType   = eventType,
                ActorUserId = actorUserId,
                DateFrom    = dateFrom,
                DateTo      = dateTo,
                Page        = page,
                PageSize    = pageSize,
            }, ct);

        return SetResponse(result?.PagedEvents);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/kits/operational-alerts</summary>
    [HttpGet("kits/operational-alerts")]
    [ProducesResponseType(typeof(CargoDryOperationalAlertsBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryOperationalAlertsBffResponse>> GetOperationalAlerts(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryOperationalAlertsBffQuery { Page = page, PageSize = pageSize }, ct);

        return SetResponse(result?.Alerts);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/operational-overview</summary>
    [HttpGet("operational-overview")]
    [ProducesResponseType(typeof(CargoDryOperationalOverviewBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryOperationalOverviewBffDto>> GetOperationalOverview(
        CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryOperationalOverviewBffQuery(), ct);

        return SetResponse(result?.Overview);
    }

    // ── Phase 11: Renewal Billing & Notification Orchestration ───────────────

    /// <summary>
    /// GET /api/v1/admin-panel/cargodry/renewals/candidates
    /// Returns Active kits expiring within WithinDays that have no open renewal preparation.
    /// Phase 11 (July 2026).
    /// </summary>
    [HttpGet("renewals/candidates")]
    [ProducesResponseType(typeof(List<CargoDryRenewalCandidateBffDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<CargoDryRenewalCandidateBffDto>>> GetRenewalCandidates(
        [FromQuery] int withinDays = 90,
        [FromQuery] int page       = 1,
        [FromQuery] int pageSize   = 50,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryRenewalCandidatesBffQuery
            {
                WithinDays = withinDays,
                Page       = page,
                PageSize   = pageSize,
            }, ct);

        return SetResponse(result?.Candidates);
    }

    /// <summary>
    /// POST /api/v1/admin-panel/cargodry/renewals
    /// Creates a new renewal preparation workflow record for a kit.
    /// Enforces one-open-preparation-per-kit. Phase 11 (July 2026).
    /// </summary>
    [HttpPost("renewals")]
    [ProducesResponseType(typeof(CargoDryRenewalPreparationBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryRenewalPreparationBffDto>> PrepareRenewal(
        [FromBody] PrepareRenewalBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new PrepareCargoDryKitRenewalBffCommand
            {
                KitId                  = body.KitId,
                RequestedRenewalMonths = body.RequestedRenewalMonths,
                PreparedByUserId       = body.PreparedByUserId,
                Note                   = body.Note,
            }, ct);

        return SetResponse(result?.Preparation);
    }

    /// <summary>
    /// GET /api/v1/admin-panel/cargodry/renewals
    /// Paginated list of renewal preparations with optional filters. Phase 11 (July 2026).
    /// </summary>
    [HttpGet("renewals")]
    [ProducesResponseType(typeof(CargoDryRenewalPreparationsPagedBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryRenewalPreparationsPagedBffResponse>> GetRenewalPreparationsPaged(
        [FromQuery] long?           kitId              = null,
        [FromQuery] string?         kitCode            = null,
        [FromQuery] string?         productCode        = null,
        [FromQuery] long?           ownerUserId        = null,
        [FromQuery] long?           vesselId           = null,
        [FromQuery] int?            status             = null,
        [FromQuery] int?            notificationStatus = null,
        [FromQuery] DateTimeOffset? preparedFrom       = null,
        [FromQuery] DateTimeOffset? preparedTo         = null,
        [FromQuery] int             page               = 1,
        [FromQuery] int             pageSize           = 25,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryRenewalPreparationsPagedBffQuery
            {
                KitId              = kitId,
                KitCode            = kitCode,
                ProductCode        = productCode,
                OwnerUserId        = ownerUserId,
                VesselId           = vesselId,
                Status             = status,
                NotificationStatus = notificationStatus,
                PreparedFrom       = preparedFrom,
                PreparedTo         = preparedTo,
                Page               = page,
                PageSize           = pageSize,
            }, ct);

        return SetResponse(result?.PagedResult);
    }

    /// <summary>
    /// GET /api/v1/admin-panel/cargodry/renewals/{id}
    /// Returns the full detail of a single renewal preparation. Phase 11 (July 2026).
    /// </summary>
    [HttpGet("renewals/{id:long}")]
    [ProducesResponseType(typeof(CargoDryRenewalPreparationBffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRenewalPreparationDetail(long id, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new GetCargoDryRenewalPreparationDetailBffQuery { Id = id }, ct);

        if (result?.Preparation is null) return NotFound();
        return Ok(SetResponse(result.Preparation));
    }

    /// <summary>
    /// POST /api/v1/admin-panel/cargodry/renewals/{id}/invoice
    /// Creates a Draft CargoDryInvoice for the renewal preparation.
    /// Does NOT create a PaymentTransaction. Idempotent. Phase 11 (July 2026).
    /// </summary>
    [HttpPost("renewals/{id:long}/invoice")]
    [ProducesResponseType(typeof(CargoDryRenewalPreparationBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryRenewalPreparationBffDto>> PrepareRenewalInvoice(
        long id, [FromBody] PrepareRenewalInvoiceBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new PrepareCargoDryRenewalInvoiceBffCommand
            {
                RenewalPreparationId = id,
                Note                 = body.Note,
            }, ct);

        return SetResponse(result?.Preparation);
    }

    /// <summary>
    /// POST /api/v1/admin-panel/cargodry/renewals/{id}/notification/prepare
    /// Stamps notification metadata on the preparation. Does NOT dispatch. Phase 11 (July 2026).
    /// </summary>
    [HttpPost("renewals/{id:long}/notification/prepare")]
    [ProducesResponseType(typeof(CargoDryRenewalPreparationBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryRenewalPreparationBffDto>> PrepareRenewalNotification(
        long id, [FromBody] PrepareRenewalNotificationBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new PrepareCargoDryRenewalNotificationBffCommand
            {
                RenewalPreparationId = id,
                TemplateCode         = body.TemplateCode,
                LanguageCode         = body.LanguageCode,
                ChannelsJson         = body.ChannelsJson,
            }, ct);

        return SetResponse(result?.Preparation);
    }

    /// <summary>
    /// POST /api/v1/admin-panel/cargodry/renewals/{id}/notification/dispatch
    /// Publishes the renewal notification message. Delivery owned by Notification module.
    /// Hard rules #2, #4, #13, #14, #15. Phase 11 (July 2026).
    /// </summary>
    [HttpPost("renewals/{id:long}/notification/dispatch")]
    [ProducesResponseType(typeof(CargoDryRenewalPreparationBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryRenewalPreparationBffDto>> DispatchRenewalNotification(
        long id, [FromBody] DispatchRenewalNotificationBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new DispatchCargoDryRenewalNotificationBffCommand
            {
                RenewalPreparationId = id,
                RecipientEmail       = body.RecipientEmail,
                RecipientPhone       = body.RecipientPhone,
            }, ct);

        return SetResponse(result?.Preparation);
    }

    /// <summary>
    /// POST /api/v1/admin-panel/cargodry/renewals/{id}/complete
    /// Explicit admin confirmation completing the renewal (calls RenewKitCommand/AdminExtension).
    /// Hard rule #8: must be explicit — not automatic. Phase 11 (July 2026).
    /// </summary>
    [HttpPost("renewals/{id:long}/complete")]
    [ProducesResponseType(typeof(CargoDryRenewalPreparationBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryRenewalPreparationBffDto>> CompleteRenewal(
        long id, [FromBody] CompleteRenewalBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new CompleteCargoDryRenewalBffCommand
            {
                RenewalPreparationId   = id,
                CompletedByUserId      = body.CompletedByUserId,
                ManualPaymentReference = body.ManualPaymentReference,
                Note                   = body.Note,
            }, ct);

        return SetResponse(result?.Preparation);
    }

    /// <summary>
    /// POST /api/v1/admin-panel/cargodry/renewals/{id}/cancel
    /// Cancels an open renewal preparation. CancellationReason is required. Phase 11 (July 2026).
    /// </summary>
    [HttpPost("renewals/{id:long}/cancel")]
    [ProducesResponseType(typeof(CargoDryRenewalPreparationBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryRenewalPreparationBffDto>> CancelRenewalPreparation(
        long id, [FromBody] CancelRenewalPreparationBodyRequest body, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new CancelCargoDryRenewalPreparationBffCommand
            {
                RenewalPreparationId = id,
                CancelledByUserId    = body.CancelledByUserId,
                CancellationReason   = body.CancellationReason,
                Note                 = body.Note,
            }, ct);

        return SetResponse(result?.Preparation);
    }
}

// ── Inline body request records ───────────────────────────────────────────────

public sealed record RevokeKitBodyRequest(string Reason);
public sealed record RevokeBatchBodyRequest(string Reason);
public sealed record TransferKitBodyRequest(long NewUserId, long NewVesselId);
public sealed record RenewKitBodyRequest(int AddedDays, string? PaymentRef);
public sealed record ExtendKitBodyRequest(int AddedDays);

public sealed class UpdateProductBody
{
    public string  Name           { get; init; } = default!;
    public string  Description    { get; init; } = default!;
    public int     ValidityDays   { get; init; }
    public decimal RetailPrice    { get; init; }
    public string  CurrencyCode   { get; init; } = default!;
    public bool    HasSmartDevice { get; init; }
    public string? DeviceType     { get; init; }
    public bool    IsActive       { get; init; }
}

public sealed record AgreementReasonBodyRequest(string Reason);

public sealed class CreateConsignmentAgreementBodyRequest
{
    public string    AgreementCode           { get; init; } = default!;
    public long      ProviderProfileId       { get; init; }
    public string    ProductCode             { get; init; } = default!;
    public decimal   ConsignmentRate         { get; init; }
    public decimal   MinimumSettlementAmount { get; init; }
    public string    CurrencyCode            { get; init; } = "TRY";
    public int       MaxKitCount             { get; init; }
    public DateTime  StartDateUtc            { get; init; }
    public DateTime? EndDateUtc              { get; init; }
    public string?   TermsDocumentRef        { get; init; }
    public string?   Notes                   { get; init; }
}

public sealed class UpdateConsignmentAgreementBodyRequest
{
    public decimal   ConsignmentRate         { get; init; }
    public decimal   MinimumSettlementAmount { get; init; }
    public string    CurrencyCode            { get; init; } = "TRY";
    public int       MaxKitCount             { get; init; }
    public DateTime  StartDateUtc            { get; init; }
    public DateTime? EndDateUtc              { get; init; }
    public string?   TermsDocumentRef        { get; init; }
    public string?   Notes                   { get; init; }
}

public sealed class AllocateBatchToProviderBodyRequest
{
    public string  BatchCode              { get; init; } = default!;
    public long    ProviderProfileId      { get; init; }
    public int     CommercialModel        { get; init; }
    public int     SalesChannel           { get; init; }
    public long?   ConsignmentAgreementId { get; init; }
    public long?   WarehouseId            { get; init; }
    public string? Note                   { get; init; }
}

public sealed class AdjustInventoryBodyRequest
{
    public long    ProviderProfileId  { get; init; }
    public string  ProductCode        { get; init; } = default!;
    public string? BatchCode          { get; init; }
    public int     AdjustmentQuantity { get; init; }
    public string  Reason             { get; init; } = default!;
}

// ── Phase 4A ─────────────────────────────────────────────────────────────────
public sealed class ResolveAttributionFinancialsBodyRequest
{
    public decimal  SalePrice              { get; init; }
    public string   CurrencyCode           { get; init; } = default!;
    public decimal? CommissionRateOverride  { get; init; }
    public long     ResolvedByUserId       { get; init; }
    public string?  ResolutionNote         { get; init; }
}

public sealed class ResolveMonthlySettlementBodyRequest
{
    public long    ResolvedByUserId { get; init; }
    public string? ResolutionNote   { get; init; }
}

// ── Phase 4B ─────────────────────────────────────────────────────────────────
public sealed class PrepareSettlementPaymentBodyRequest
{
    public long    PreparedByUserId { get; init; }
    public string? PreparationNote  { get; init; }
}

// ── Phase 4C ─────────────────────────────────────────────────────────────────
public sealed class PrepareSettlementInvoiceBodyRequest
{
    public long    PreparedByUserId { get; init; }
    public string? PreparationNote  { get; init; }
}

// ── Phase 6 ──────────────────────────────────────────────────────────────────
public sealed class RunSettlementAutomationBodyRequest
{
    public int    TargetYearMonth    { get; init; }
    public int    Mode               { get; init; } = 1; // DryRun default
    public bool   AutoPreparePayment { get; init; } = false;
    public bool   AutoPrepareInvoice { get; init; } = false;
    public long   TriggeredByUserId  { get; init; }
    public string? Note              { get; init; }
}

// ── Phase 4D ─────────────────────────────────────────────────────────────────
public sealed class ApproveSettlementPayoutBodyRequest
{
    public long    ApprovedByUserId { get; init; }
    public string? Note             { get; init; }
}

public sealed class MarkSettlementPayoutProcessingBodyRequest
{
    public long    ProcessedByUserId  { get; init; }
    public string? ExternalReference  { get; init; }
    public string? Note               { get; init; }
}

public sealed class CompleteSettlementPayoutBodyRequest
{
    public long    CompletedByUserId      { get; init; }
    public string  ManualPaymentReference { get; init; } = default!;
    public string? Note                   { get; init; }
}

public sealed class FailSettlementPayoutBodyRequest
{
    public long    FailedByUserId    { get; init; }
    public string  FailureReason     { get; init; } = default!;
    public string? ExternalReference { get; init; }
    public string? Note              { get; init; }
}

// ── Phase 11: Renewal Billing & Notification Orchestration ───────────────────
public sealed class PrepareRenewalBodyRequest
{
    public long    KitId                  { get; init; }
    public int     RequestedRenewalMonths { get; init; }
    public long    PreparedByUserId       { get; init; }
    public string? Note                   { get; init; }
}

public sealed class PrepareRenewalInvoiceBodyRequest
{
    public string? Note { get; init; }
}

public sealed class PrepareRenewalNotificationBodyRequest
{
    public string TemplateCode { get; init; } = default!;
    public string LanguageCode { get; init; } = "tr";
    public string ChannelsJson { get; init; } = "[\"Email\"]";
}

public sealed class DispatchRenewalNotificationBodyRequest
{
    public string? RecipientEmail { get; init; }
    public string? RecipientPhone { get; init; }
}

public sealed class CompleteRenewalBodyRequest
{
    public long    CompletedByUserId      { get; init; }
    public string? ManualPaymentReference { get; init; }
    public string? Note                   { get; init; }
}

public sealed class CancelRenewalPreparationBodyRequest
{
    public long    CancelledByUserId  { get; init; }
    public string  CancellationReason { get; init; } = default!;
    public string? Note               { get; init; }
}
