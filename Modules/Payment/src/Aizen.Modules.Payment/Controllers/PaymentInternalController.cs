using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Application.Commands.CreatePaymentEscrow;
using Aizen.Modules.Payment.Application.Commands.ReleasePaymentEscrow;
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
}
