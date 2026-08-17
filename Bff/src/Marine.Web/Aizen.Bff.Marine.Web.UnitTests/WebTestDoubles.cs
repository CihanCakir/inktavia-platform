using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Common.Services;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Abstraction.Model;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Abstraction.Dto.ProviderEligibility;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

namespace Aizen.Bff.Marine.Web.UnitTests;

// ── Notification (contact intake) remote stub ─────────────────────────────────────

internal sealed class FakeNotificationRemoteCall : INotificationRemoteCall
{
    public AizenApiResponse<SubmitContactResponse>? Response { get; set; }
    public SubmitContactRequest? LastRequest { get; private set; }
    public string? LastForwardedFor { get; private set; }

    public Task<AizenApiResponse<SubmitContactResponse>> SubmitContact(SubmitContactRequest request, string? forwardedFor)
    {
        LastRequest = request;
        LastForwardedFor = forwardedFor;
        return Task.FromResult(Response ?? new AizenApiResponse<SubmitContactResponse>
        {
            Body = new SubmitContactResponse { Accepted = true, TicketRef = "CT-TEST123456" },
        });
    }
}

// ── Helpers ────────────────────────────────────────────────────────────────────

internal static class Env
{
    public static AizenApiResponse<T> Ok<T>(T body) where T : class => new(AizenResponseHeader.Success(), body);
}

/// <summary>Builds a real Refit <see cref="Refit.ApiException"/> with a given status (for the clean-error path).</summary>
internal static class RefitFault
{
    public static async Task<Refit.ApiException> Of(System.Net.HttpStatusCode code)
        => await Refit.ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "http://module"),
            HttpMethod.Get,
            new HttpResponseMessage(code),
            new Refit.RefitSettings());
}

// ── Content remote stub ──────────────────────────────────────────────────────────
// Captures the surface passed to the public feed/by-type (to prove MarineOsWeb pinning), returns canned bodies,
// or throws a supplied Refit exception. Methods not under test throw if ever called.

internal sealed class FakeContentRemoteCall : IContentRemoteCall
{
    public ContentSurface? LastFeedSurface { get; private set; }
    public ContentSurface? LastByTypeSurface { get; private set; }

    public ContentFeedResponse? FeedResponse { get; set; }
    public ContentItemDto? BySlugResponse { get; set; }
    public ContentCommentsResponse? PublicCommentsResponse { get; set; }
    public AizenApiResponse<ContentCommentDto>? AddCommentResponse { get; set; }
    public AizenApiResponse<ContentFavoritesResponse>? MyFavoritesResponse { get; set; }
    public AizenApiResponse<List<ContentCommentDto>>? MyCommentsResponse { get; set; }

    public Exception? ThrowOnBySlug { get; set; }
    public Exception? ThrowOnAddComment { get; set; }

    public Task<ContentFeedResponse> GetPublicFeed(ContentSurface surface, string lang, int page, int pageSize)
    {
        LastFeedSurface = surface;
        return Task.FromResult(FeedResponse!);
    }

    public Task<ContentFeedResponse> GetPublicByType(ContentSurface surface, ContentType type, string lang, int page, int pageSize)
    {
        LastByTypeSurface = surface;
        return Task.FromResult(FeedResponse!);
    }

    public Task<ContentItemDto> GetPublicBySlug(string slug, string lang)
    {
        if (ThrowOnBySlug is not null) throw ThrowOnBySlug;
        return Task.FromResult(BySlugResponse!);
    }

    public Task<List<ContentCategoryDto>> GetPublicCategories(string lang) => throw new NotImplementedException();

    public Task<ContentCommentsResponse> GetPublicComments(string contentId, int page, int pageSize)
        => Task.FromResult(PublicCommentsResponse!);

    public Task<AizenApiResponse<ContentCommentDto>> AddComment(string contentId, AddContentCommentRequest request)
    {
        if (ThrowOnAddComment is not null) throw ThrowOnAddComment;
        return Task.FromResult(AddCommentResponse!);
    }

    public Task<AizenApiResponse<ContentFavoriteResultDto>> AddFavorite(string contentId) => throw new NotImplementedException();
    public Task<AizenApiResponse<ContentFavoriteResultDto>> RemoveFavorite(string contentId) => throw new NotImplementedException();

    public Task<AizenApiResponse<ContentFavoritesResponse>> GetMyFavorites(string lang, int page, int pageSize)
        => Task.FromResult(MyFavoritesResponse!);

    public Task<AizenApiResponse<List<ContentCommentDto>>> GetMyComments(string contentId)
        => Task.FromResult(MyCommentsResponse!);
}

// ── ReferenceData remote stub ─────────────────────────────────────────────────────

internal sealed class FakeReferenceDataRemoteCall : IReferenceDataRemoteCall
{
    public AizenApiResponse<List<CountryDto>>? CountriesResponse { get; set; }
    public AizenApiResponse<List<CityDto>>? CitiesResponse { get; set; }
    public AizenApiResponse<List<LookupItemDto>>? LookupsResponse { get; set; }
    public Exception? ThrowOnCities { get; set; }

    public string? LastCitiesCountryCode { get; private set; }

    // W4 location-detail stubs (single reads + districts list). Settable per test; default to a null-body response.
    public AizenApiResponse<CountryDto?>? CountryResponse { get; set; }
    public AizenApiResponse<CityDto?>? CityResponse { get; set; }
    public AizenApiResponse<List<DistrictDto>>? DistrictsResponse { get; set; }

    public Task<AizenApiResponse<List<CountryDto>>> GetCountries(bool onlyActive = true)
        => Task.FromResult(CountriesResponse!);

    public Task<AizenApiResponse<CountryDto?>> GetCountry(string countryCode)
        => Task.FromResult(CountryResponse ?? new AizenApiResponse<CountryDto?>());

    public Task<AizenApiResponse<List<CityDto>>> GetCitiesByCountry(string countryCode, bool onlyActive = true)
    {
        LastCitiesCountryCode = countryCode;
        if (ThrowOnCities is not null) throw ThrowOnCities;
        return Task.FromResult(CitiesResponse!);
    }

    public Task<AizenApiResponse<CityDto?>> GetCity(string countryCode, string cityCode)
        => Task.FromResult(CityResponse ?? new AizenApiResponse<CityDto?>());

    public Task<AizenApiResponse<List<DistrictDto>>> GetDistrictsByCity(string countryCode, string cityCode, bool onlyActive = true)
        => Task.FromResult(DistrictsResponse ?? new AizenApiResponse<List<DistrictDto>>());

    public Task<AizenApiResponse<List<LookupItemDto>>> GetLookupItems(string groupCode, bool onlyActive = true)
        => Task.FromResult(LookupsResponse!);

    // M3 by-slug resolver stub — settable per test; defaults to a null-body response.
    public AizenApiResponse<LocationBySlugDto?>? LocationBySlugResponse { get; set; }
    public string? LastBySlug { get; private set; }

    public Task<AizenApiResponse<LocationBySlugDto?>> GetLocationBySlug(string slug)
    {
        LastBySlug = slug;
        return Task.FromResult(LocationBySlugResponse ?? new AizenApiResponse<LocationBySlugDto?>());
    }
}

// ── Identity remote stub ──────────────────────────────────────────────────────────
// Records the holder's Resolved state AT THE MOMENT it is called — this proves the resolver's own Identity call
// runs BEFORE the holder is populated (no assertion attached → no recursion).

internal sealed class FakeIdentityRemoteCall : IIdentityRemoteCall
{
    private readonly IWebIdentityHolder? _holderToObserve;
    private readonly OrganizerProfileDetailDto? _profile;

    public FakeIdentityRemoteCall(OrganizerProfileDetailDto? profile, IWebIdentityHolder? holderToObserve = null)
    {
        _profile = profile;
        _holderToObserve = holderToObserve;
    }

    public bool WasCalled { get; private set; }
    public bool HolderWasResolvedWhenCalled { get; private set; }

    public Task<AizenApiResponse<OrganizerProfileDetailDto>> GetParticipantProfileByKeycloakSubject(string keycloakSubject)
    {
        WasCalled = true;
        HolderWasResolvedWhenCalled = _holderToObserve?.Resolved ?? false;
        return Task.FromResult(_profile is null
            ? new AizenApiResponse<OrganizerProfileDetailDto>()   // null body ⇒ unlinked
            : Env.Ok(_profile));
    }

    // M2 availability stub — settable per test; defaults to a null-body response.
    public AizenApiResponse<ProviderAreaAvailabilityDto>? AvailabilityResponse { get; set; }
    public string? LastAvailabilityCity { get; private set; }
    public string? LastAvailabilityCategory { get; private set; }

    public Task<AizenApiResponse<ProviderAreaAvailabilityDto>> GetProviderAreaAvailability(string cityCode, string? categoryCode = null)
    {
        LastAvailabilityCity = cityCode;
        LastAvailabilityCategory = categoryCode;
        return Task.FromResult(AvailabilityResponse ?? new AizenApiResponse<ProviderAreaAvailabilityDto>());
    }
}

// ── Participant context stub ──────────────────────────────────────────────────────

internal sealed class FakeWebParticipantContext : IWebParticipantContext
{
    public FakeWebParticipantContext(string? subject) => KeycloakSubject = subject;

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(KeycloakSubject);
    public string? KeycloakSubject { get; }
    public string? Email => null;
    public string? PreferredUsername => null;
    public string? FirstName => null;
    public string? LastName => null;
    public bool EmailVerified => false;
    public long? ParticipantProfileId => null;
    public bool HasProfileLink => false;
    public IReadOnlyList<string> Roles => new List<string>();
    public bool IsInRole(string role) => false;
}

/// <summary>Resolver stub for handler-level tests — returns a fixed resolution so the identity gate is deterministic.</summary>
internal sealed class FakeWebParticipantProfileResolver : IWebParticipantProfileResolver
{
    private readonly WebParticipantProfileResolution _resolution;
    public FakeWebParticipantProfileResolver(long? userId, long? profileId = null)
        => _resolution = new WebParticipantProfileResolution(userId, profileId, "test");

    public Task<WebParticipantProfileResolution> ResolveAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_resolution);
}

/// <summary>SEO policy stub — returns a fixed verdict so handler tests are deterministic (real thresholds are W5).</summary>
internal sealed class FakeSeoIndexabilityPolicy : Aizen.Bff.Marine.Web.Application.Common.Seo.ISeoIndexabilityPolicy
{
    private readonly Aizen.Bff.Marine.Web.Application.Common.Seo.SeoVerdict _verdict;
    public FakeSeoIndexabilityPolicy(bool indexable = true, string reason = "indexable")
        => _verdict = new Aizen.Bff.Marine.Web.Application.Common.Seo.SeoVerdict(indexable, reason);

    public Aizen.Bff.Marine.Web.Application.Common.Seo.SeoVerdict Evaluate(
        Aizen.Bff.Marine.Web.Application.Common.Seo.SeoEvaluationInput input) => _verdict;
}
