using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Trip;

namespace Aizen.Bff.MarineProvider.Application.Trips;

/// <summary>
/// Provider trip ingestion handlers. Each resolves the caller (sets the identity holder so the S2S assertion stamps
/// the provider's ProviderProfileId) then forwards to the SR module, which enforces the assigned-provider + accepted-
/// job guard and the ping throttle. Thin passthrough — no trip logic on the BFF.
/// </summary>
internal static class TripBffGuard
{
    public static async Task EnsureProviderAsync(IProviderProfileResolver resolver, IProviderIdentityHolder holder, CancellationToken ct)
    {
        await resolver.ResolveAsync(ct);
        if (holder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
    }

    public static TripActionResponse Unwrap(Core.Infrastructure.Api.AizenApiResponse<TripActionResponse>? resp)
    {
        if (resp?.Header?.IsSuccess != true || resp.Body is null)
            throw new AizenBusinessException(resp?.Header?.ErrorMessage ?? "Trip action failed.");
        return resp.Body;
    }
}

public sealed class StartTripBffCommandHandler : AizenCommandHandler<StartTripBffCommand, TripActionResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;

    public StartTripBffCommandHandler(IProviderProfileResolver resolver, IProviderIdentityHolder holder, IServiceRequestRemoteCall sr)
    {
        _resolver = resolver; _holder = holder; _sr = sr;
    }

    public override async Task<TripActionResponse?> Handle(StartTripBffCommand request, CancellationToken ct)
    {
        await TripBffGuard.EnsureProviderAsync(_resolver, _holder, ct);
        return TripBffGuard.Unwrap(await _sr.StartTrip(request.ServiceRequestId));
    }
}

public sealed class PingTripBffCommandHandler : AizenCommandHandler<PingTripBffCommand, TripActionResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;

    public PingTripBffCommandHandler(IProviderProfileResolver resolver, IProviderIdentityHolder holder, IServiceRequestRemoteCall sr)
    {
        _resolver = resolver; _holder = holder; _sr = sr;
    }

    public override async Task<TripActionResponse?> Handle(PingTripBffCommand request, CancellationToken ct)
    {
        var body = request.Body ?? throw new AizenBusinessException("Location payload is required.");
        await TripBffGuard.EnsureProviderAsync(_resolver, _holder, ct);
        return TripBffGuard.Unwrap(await _sr.PingTrip(request.ServiceRequestId, body));
    }
}

public sealed class ArriveTripBffCommandHandler : AizenCommandHandler<ArriveTripBffCommand, TripActionResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;

    public ArriveTripBffCommandHandler(IProviderProfileResolver resolver, IProviderIdentityHolder holder, IServiceRequestRemoteCall sr)
    {
        _resolver = resolver; _holder = holder; _sr = sr;
    }

    public override async Task<TripActionResponse?> Handle(ArriveTripBffCommand request, CancellationToken ct)
    {
        await TripBffGuard.EnsureProviderAsync(_resolver, _holder, ct);
        return TripBffGuard.Unwrap(await _sr.ArriveTrip(request.ServiceRequestId));
    }
}

public sealed class CancelTripBffCommandHandler : AizenCommandHandler<CancelTripBffCommand, TripActionResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;

    public CancelTripBffCommandHandler(IProviderProfileResolver resolver, IProviderIdentityHolder holder, IServiceRequestRemoteCall sr)
    {
        _resolver = resolver; _holder = holder; _sr = sr;
    }

    public override async Task<TripActionResponse?> Handle(CancelTripBffCommand request, CancellationToken ct)
    {
        await TripBffGuard.EnsureProviderAsync(_resolver, _holder, ct);
        return TripBffGuard.Unwrap(await _sr.CancelTrip(request.ServiceRequestId));
    }
}
