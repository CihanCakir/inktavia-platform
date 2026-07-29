using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Commands.RefundAllocationPolicyAdmin;
using Aizen.Modules.Payment.Application.ProviderBalanceAdmin;
using Aizen.Modules.Payment.Application.Queries.RefundAllocationPolicyAdmin;
using Aizen.Modules.Payment.Application.Queries.RefundChargebackQueue;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

/// <summary>
/// BE-P10 admin surface: RefundAllocationPolicy CRUD, refund/chargeback queues, and the provider negative-balance ledger
/// + audited manual adjustment. Read-only over the immutable P10 records; the policy is the single tunable.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/admin")]
public sealed class RefundAdminController : ControllerBase
{
    private readonly ISender _sender;
    public RefundAdminController(ISender sender) => _sender = sender;

    // ── RefundAllocationPolicy CRUD ───────────────────────────────────────────

    [HttpGet("refund-allocation-policies")]
    public async Task<IActionResult> GetPolicies(CancellationToken ct)
        => Ok(await _sender.Send(new GetRefundAllocationPoliciesQuery(), ct));

    [HttpGet("refund-allocation-policies/resolve")]
    public async Task<IActionResult> ResolvePolicy([FromQuery] string currencyCode = "TRY", [FromQuery] DateTime? atUtc = null, CancellationToken ct = default)
        => Ok(await _sender.Send(new ResolveRefundAllocationPolicyQuery { CurrencyCode = currencyCode, AtUtc = atUtc }, ct));

    [HttpGet("refund-allocation-policies/{id:long}")]
    public async Task<IActionResult> GetPolicy(long id, CancellationToken ct)
        => Ok(await _sender.Send(new GetRefundAllocationPolicyByIdQuery { Id = id }, ct));

    [HttpPost("refund-allocation-policies")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreateRefundAllocationPolicyCommand command, CancellationToken ct)
        => Ok(await _sender.Send(command, ct));

    [HttpPut("refund-allocation-policies/{id:long}")]
    public async Task<IActionResult> UpdatePolicy(long id, [FromBody] UpdateRefundAllocationPolicyCommand body, CancellationToken ct)
        => Ok(await _sender.Send(new UpdateRefundAllocationPolicyCommand
        {
            Id = id, NegativeBalanceLimit = body.NegativeBalanceLimit,
            EffectiveFrom = body.EffectiveFrom, EffectiveTo = body.EffectiveTo, Notes = body.Notes,
        }, ct));

    [HttpPost("refund-allocation-policies/{id:long}/deactivate")]
    public async Task<IActionResult> DeactivatePolicy(long id, CancellationToken ct)
        => Ok(await _sender.Send(new DeactivateRefundAllocationPolicyCommand { Id = id }, ct));

    // ── Refund / chargeback queues ────────────────────────────────────────────

    [HttpGet("refund-queue")]
    public async Task<IActionResult> GetRefundQueue(
        [FromQuery] RefundCause? cause = null, [FromQuery] ReleaseState? releaseState = null,
        [FromQuery] TransactionRefundStatus? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetRefundQueueQuery { Cause = cause, ReleaseState = releaseState, Status = status, Page = page, PageSize = pageSize }, ct));

    [HttpGet("chargeback-queue")]
    public async Task<IActionResult> GetChargebackQueue([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetChargebackQueueQuery { Page = page, PageSize = pageSize }, ct));

    // ── Provider negative-balance ledger + manual adjust ──────────────────────

    [HttpGet("provider-balances")]
    public async Task<IActionResult> GetProviderBalances(
        [FromQuery] string? currency = null, [FromQuery] bool onlyNegative = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetProviderBalancesQuery { Currency = currency, OnlyNegative = onlyNegative, Page = page, PageSize = pageSize }, ct));

    [HttpGet("provider-balances/{providerProfileId:long}")]
    public async Task<IActionResult> GetProviderBalance(long providerProfileId, [FromQuery] string currency = "TRY", CancellationToken ct = default)
        => Ok(await _sender.Send(new GetProviderBalanceByProviderQuery { ProviderProfileId = providerProfileId, Currency = currency }, ct));

    [HttpPost("provider-balances/{providerProfileId:long}/adjust")]
    public async Task<IActionResult> AdjustProviderBalance(long providerProfileId, [FromBody] AdjustProviderBalanceCommand body, CancellationToken ct)
        => Ok(await _sender.Send(new AdjustProviderBalanceCommand
        {
            ProviderProfileId = providerProfileId, CurrencyCode = body.CurrencyCode,
            SignedAmount = body.SignedAmount, AdminUserId = body.AdminUserId, Note = body.Note,
        }, ct));
}
