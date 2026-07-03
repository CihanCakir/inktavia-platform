using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.ActivateConsignmentAgreement;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySalesAttributionDetail;
using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySalesAttributionsPaged;
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
