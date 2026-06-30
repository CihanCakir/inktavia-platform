using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Offer;

[DocumentationInfo("Get provider offers query handler", "Maps service request offers to BFF-friendly DTOs.")]
public sealed class GetProviderOffersQueryHandler : AizenQueryHandler<GetProviderOffersQuery, GetProviderOffersResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;

    public GetProviderOffersQueryHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository)
    {
        _srRepository = srRepository;
        _offerRepository = offerRepository;
    }

    public override async Task<GetProviderOffersResponse> Handle(GetProviderOffersQuery request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdWithDetailsAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var offers = sr.Offers.Select(o => new ProviderOfferItemDto
        {
            Id = o.Id.ToString(),
            ProviderId = o.ProviderProfileId.ToString(),
            ProviderName = $"Provider {o.ProviderProfileId}",
            Rating = 0,
            ReviewCount = 0,
            QuoteAmount = o.TotalAmount,
            Currency = o.CurrencyCode,
            EstimatedDuration = o.EstimatedDurationMinutes.HasValue
                ? $"{o.EstimatedDurationMinutes / 60}h {o.EstimatedDurationMinutes % 60}m"
                : string.Empty,
            ProposalUrl = null,
            Status = MapOfferStatus(o.Status.ToString()),
            SubmittedAt = o.CreateDate.HasValue
                ? new DateTimeOffset(o.CreateDate.Value, TimeSpan.Zero)
                : DateTimeOffset.UtcNow
        }).ToList();

        var agreementTimeline = sr.StatusHistory
            .Where(h => h.ToStatus is
                Abstraction.Enum.ServiceRequestStatus.OfferAccepted or
                Abstraction.Enum.ServiceRequestStatus.Completed or
                Abstraction.Enum.ServiceRequestStatus.Closed)
            .Select((h, i) => new AgreementEventDto
            {
                Id = (i + 1).ToString(),
                Timestamp = new DateTimeOffset(h.OccurredAt, TimeSpan.Zero),
                Title = h.ToStatus.ToString(),
                Description = h.Reason,
                Actor = h.ActorUserId?.ToString(),
                Icon = "event"
            })
            .ToList();

        return new GetProviderOffersResponse(sr.Id.ToString(), sr.Title, offers, agreementTimeline);
    }

    private static string MapOfferStatus(string status) => status switch
    {
        "Accepted" => "Accepted",
        "Rejected" => "Rejected",
        "Withdrawn" => "Rejected",
        "Submitted" => "Pending",
        _ => "Pending"
    };
}
