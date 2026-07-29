using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Application.Commands.CalculateServiceRequestEconomics;
using Aizen.Modules.Payment.Application.Commands.CreatePaymentEscrow;
using Aizen.Modules.Payment.Application.Commands.ReleasePaymentEscrow;
using Aizen.Modules.Payment.Application.Queries.GetProviderSplitEligibility;
using Aizen.Modules.Payment.Application.Queries.ResolveCustomerDiscountForOffer;
using Aizen.Modules.Payment.Application.Queries.ResolveLineCommissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

/// <summary>
/// Internal (service-to-service) endpoints for the Payment module.
/// Protected by [Authorize] (valid JWT required), NOT restricted to Admin role.
/// These endpoints are called by other Inktavia modules (e.g. ServiceRequest) as internal
/// orchestration calls — they must not be exposed in the public API gateway.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/payment/internal")]
public sealed class PaymentInternalController : ControllerBase
{
    private readonly ISender _sender;
    public PaymentInternalController(ISender sender) => _sender = sender;

    /// <summary>
    /// Creates an escrow transaction from an inter-module call.
    /// Idempotent — duplicate requests with the same IdempotencyKey return the existing result.
    /// Called by ServiceRequest module after offer acceptance.
    /// </summary>
    [HttpPost("escrow")]
    public async Task<IActionResult> CreateEscrow(
        [FromBody] CreateEscrowRemoteCallRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CreatePaymentEscrowCommand
        {
            IdempotencyKey     = request.IdempotencyKey,
            Context            = request.Context,
            TransactionType    = request.TransactionType,
            PayerProfileId     = request.PayerProfileId,
            RecipientProfileId = request.RecipientProfileId,
            GrossAmount        = request.GrossAmount,
            DiscountAmount     = request.DiscountAmount,
            CurrencyCode       = request.CurrencyCode,
            ProviderPlanId     = request.ProviderPlanId,
            CategoryCode       = request.CategoryCode,
            EscrowRequired     = request.EscrowRequired,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Releases the escrow for a specific transaction.
    /// Creates a PayoutRecord for the provider.
    /// Called by ServiceRequest module when admin approves SR completion.
    /// </summary>
    [HttpPost("transactions/{transactionId:long}/release")]
    public async Task<IActionResult> ReleaseEscrow(
        long transactionId,
        [FromBody] ReleaseEscrowRemoteCallRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ReleasePaymentEscrowCommand
        {
            TransactionId    = transactionId,
            ApprovedByUserId = request.ApprovedByUserId,
            AdminNote        = request.AdminNote,
        }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Resolves per-line commissions for an offer's priced lines (BE-S7). Read-only / idempotent — pure resolution,
    /// no state written, no applied-count bump. Called by ServiceRequest as an offer-builder preview.
    /// </summary>
    [HttpPost("commission/resolve-lines")]
    public async Task<IActionResult> ResolveLineCommissions(
        [FromBody] Abstraction.RemoteCall.Requests.ResolveLineCommissionsRemoteCallRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new ResolveLineCommissionsQuery { Request = request }, ct));

    /// <summary>
    /// BE-P8 — acceptance-time economics: runs the §19.9 combiner and, on an approving decision, creates the escrow
    /// (gross = CustomerTotal, split = ProviderNet) + immutable snapshot and bumps applied-count once. Idempotent.
    /// Called by ServiceRequest at offer acceptance; on Rejected/ConfigurationError the caller blocks acceptance.
    /// </summary>
    [HttpPost("service-request/calculate-economics")]
    public async Task<IActionResult> CalculateServiceRequestEconomics(
        [FromBody] CalculateServiceRequestEconomicsRemoteCallRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new CalculateServiceRequestEconomicsCommand { Request = request }, ct));

    /// <summary>
    /// BE-I1 — reads whether a provider has a split-eligible sub-merchant (the P9 gate signal). Pure read. Called by
    /// ServiceRequest to surface/flag a non-payable provider before the hard acceptance gate.
    /// </summary>
    [HttpPost("provider/split-eligibility")]
    public async Task<IActionResult> GetProviderSplitEligibility(
        [FromBody] GetProviderSplitEligibilityRemoteCallRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new GetProviderSplitEligibilityQuery { Request = request }, ct));

    /// <summary>
    /// BE-S6 — resolves the P6 customer-discount rule + requested amount + funding split for an offer. Pure read /
    /// compute-on-demand (no persistence/budget/snapshot). Called by ServiceRequest's offer-builder discount preview.
    /// </summary>
    [HttpPost("discount/resolve-customer-discount")]
    public async Task<IActionResult> ResolveCustomerDiscount(
        [FromBody] ResolveCustomerDiscountRemoteCallRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new ResolveCustomerDiscountForOfferQuery { Request = request }, ct));
}
