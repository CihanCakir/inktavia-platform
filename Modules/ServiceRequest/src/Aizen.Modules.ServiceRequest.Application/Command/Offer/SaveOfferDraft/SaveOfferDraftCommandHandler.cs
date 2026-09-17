using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer.SaveOfferDraft;

/// <summary>
/// Aggregate save: get-or-create the caller's Draft for the request, ReplaceItems, set commercial terms,
/// run the calculation service, persist. Optimistic concurrency via xmin token.
/// One active offer per (provider, request).
/// Flushes SaveChanges explicitly so the generated Id is in the response.
/// </summary>
public sealed class SaveOfferDraftCommandHandler : AizenCommandHandler<SaveOfferDraftCommand, SaveOfferDraftResponse>
{
    private static readonly HashSet<ServiceRequestStatus> BiddableStatuses = new()
    {
        ServiceRequestStatus.Open,
        ServiceRequestStatus.WaitingForOffer,
        ServiceRequestStatus.OfferReceived,
    };

    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly OfferCalculationService _calculation;
    private readonly UnitCodeValidator _unitCodeValidator;
    private readonly ServiceRequestDbContext _db;

    public SaveOfferDraftCommandHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info,
        OfferCalculationService calculation,
        UnitCodeValidator unitCodeValidator,
        ServiceRequestDbContext db)
    {
        _srRepository = srRepository;
        _offerRepository = offerRepository;
        _info = info;
        _calculation = calculation;
        _unitCodeValidator = unitCodeValidator;
        _db = db;
    }

    public override async Task<SaveOfferDraftResponse?> Handle(SaveOfferDraftCommand command, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var sr = await _srRepository.GetByIdAsync(command.ServiceRequestId, ct)
            ?? throw new AizenBusinessException("Service request not found.");

        if (!BiddableStatuses.Contains(sr.Status))
            throw new AizenBusinessException("SR_OFFER_REQUEST_CLOSED");

        var req = command.Request;
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;

        ValidateItems(req);
        await _unitCodeValidator.ValidateUnitCodesAsync(req.Items, ct);

        var offer = await _offerRepository.GetDraftByProviderAndRequestAsync(providerProfileId, sr.Id, ct);
        bool isNew = offer is null;

        if (isNew)
        {
            offer = ServiceRequestOfferEntity.Create(
                sr.Id, providerProfileId, currentUserId,
                0, req.CurrencyCode, req.Description, req.ProviderNotes,
                req.EstimatedStartDate, req.EstimatedEndDate, req.EstimatedDurationMinutes, req.ExpiresAt);
        }
        else
        {
            offer!.Update(0, req.CurrencyCode, req.Description, req.ProviderNotes,
                req.EstimatedStartDate, req.EstimatedEndDate, req.EstimatedDurationMinutes, req.ExpiresAt);
        }

        offer.UpdateCommercialTerms(req.DepositType, req.DepositValue, req.PaymentTermsNote, req.WarrantyNote);

        var newItems = req.Items.Select((item, idx) => ServiceRequestOfferItemEntity.Create(
            0, item.ItemType, item.Title, item.Description,
            item.Quantity, item.UnitPrice, item.CurrencyCode, item.SortOrder > 0 ? item.SortOrder : idx,
            item.UnitCode, item.TaxRate, item.DiscountType, item.DiscountValue,
            item.PricingMethod, item.CommissionEligibility
        )).ToList();

        offer.ReplaceItems(newItems);
        _calculation.Calculate(offer);

        if (isNew)
            await _offerRepository.AddAsync(offer, ct);
        else
            _offerRepository.Update(offer);

        // ── Optimistic concurrency (SR_OFFER_STALE) ────────────────────────────
        // On an EXISTING draft, force the tracked entity's ORIGINAL xmin to the client's last-read token so the UPDATE's
        // WHERE clause carries `xmin = <token>`. If another save happened since, the row's xmin has advanced → 0 rows
        // affected → DbUpdateConcurrencyException → SR_OFFER_STALE. Without this the tracked entity always carries the
        // CURRENT xmin (freshly loaded above), so a stale tab could only lose a same-instant race — never a real stale
        // save. Rules:
        //   • unparsable token (e.g. a legacy offer-id token handed out before this change) → treat as stale, NEVER as
        //     "skip the check" — a mid-deploy editor gets one stale warning and reloads.
        //   • null/empty token → keep today's last-write-wins (backward compatible during rollout / first create).
        if (!isNew && !string.IsNullOrEmpty(req.ConcurrencyToken))
        {
            if (!uint.TryParse(req.ConcurrencyToken, out var expectedXmin))
                throw new AizenBusinessException("SR_OFFER_STALE");

            _db.Entry(offer!).Property<uint>("xmin").OriginalValue = expectedXmin;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AizenBusinessException("SR_OFFER_STALE");
        }

        // Return the CURRENT xmin (EF refreshes it from the DB via RETURNING after SaveChanges) — never the offer id,
        // which never changes and made the token useless.
        var concurrencyToken = _db.Entry(offer!).Property<uint>("xmin").CurrentValue.ToString();
        return new SaveOfferDraftResponse(offer.ToDto(), concurrencyToken);
    }

    private static void ValidateItems(Abstraction.Request.Offer.SaveOfferDraftRequest req)
    {
        if (req.Items.Count > 100)
            throw new AizenBusinessException("Maximum 100 items per offer.");

        // BE-S3 — mixed source currencies are ALLOWED: a line may be quoted in a foreign currency and is converted to the
        // settlement currency (TRY) at offer-submit via the R1 point-in-time rate. The old single-currency guard is dropped.
        foreach (var item in req.Items)
        {
            if (item.ItemType != ServiceRequestOfferItemType.Discount)
            {
                if (item.Quantity <= 0)
                    throw new AizenBusinessException($"Quantity must be > 0 for item '{item.Title}'.");
                if (item.UnitPrice < 0)
                    throw new AizenBusinessException($"Unit price must be >= 0 for item '{item.Title}'.");
            }
            if (item.TaxRate < 0 || item.TaxRate > 1)
                throw new AizenBusinessException($"Tax rate must be between 0 and 1 for item '{item.Title}'.");

            // ── BE-S1: new line-economics inputs must be defined enum values ──
            if (!Enum.IsDefined(typeof(PricingMethod), item.PricingMethod))
                throw new AizenBusinessException($"PricingMethod is invalid for item '{item.Title}'.");
            if (item.CommissionEligibility is { } elig && !Enum.IsDefined(typeof(LineCommissionEligibility), elig))
                throw new AizenBusinessException($"CommissionEligibility is invalid for item '{item.Title}'.");
        }
    }
}
