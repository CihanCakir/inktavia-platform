using Aizen.Core.Cache.Abstraction;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Application.Command.Offer.SaveOfferDraft;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Aizen.Modules.ServiceRequest.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// SR_OFFER_STALE optimistic concurrency on the offer-draft save.
///
/// The EF InMemory provider DOES enforce a concurrency token by comparing the entry's OriginalValue against the stored
/// value, so the real stale path is exercised here: a token that differs from the persisted xmin raises
/// DbUpdateConcurrencyException → SR_OFFER_STALE and does NOT overwrite; a matching token saves. The parse-fail→stale
/// and null-token compat rules are covered too. LIMITATION: InMemory does not GENERATE/advance xmin the way Postgres
/// does (via RETURNING), so the response token does not necessarily change value between saves on this provider — that
/// aspect is Postgres-only; here we assert the response token is the current xmin string, never the offer id.
/// </summary>
public sealed class SaveOfferDraftConcurrencyTests
{
    private const long ProviderProfileId = 9001;
    private const long ProviderUserId    = 7;

    private static ServiceRequestDbContext NewDb(string name)
        => new(new DbContextOptionsBuilder<ServiceRequestDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    private static IAizenInfoAccessor Info()
    {
        var info = Substitute.For<IAizenInfoAccessor>();
        var kc = Substitute.For<IAizenKeycloakTokenInfoAccessor>();
        kc.KeycloakTokenInfo.Returns(new AizenKeycloakTokenInfo { ProviderProfileId = ProviderProfileId });
        info.KeycloakTokenInfoAccessor.Returns(kc);
        var users = Substitute.For<IAizenUserInfoAccessor>();
        users.UserInfo.Returns(new AizenUserInfo { UserId = ProviderUserId });
        info.UserInfoAccessor.Returns(users);
        return info;
    }

    private static SaveOfferDraftCommandHandler Handler(ServiceRequestDbContext db)
    {
        var unitCode = new UnitCodeValidator(
            Substitute.For<IServiceRequestReferenceDataRemoteCall>(),
            Substitute.For<IAizenDistributedCache>(),
            NullLogger<UnitCodeValidator>.Instance);

        return new SaveOfferDraftCommandHandler(
            new ServiceRequestRepository(db),
            new ServiceRequestOfferRepository(db),
            Info(),
            new OfferCalculationService(),
            unitCode,
            db);
    }

    private static async Task<long> SeedBiddableSrAsync(ServiceRequestDbContext db)
    {
        var sr = ServiceRequestEntity.Create(
            requestCode: "SR-CONC", ownerUserId: 100, vesselId: 5,
            serviceCategoryCode: "MECH", serviceTypeCode: null, title: "Engine", description: null,
            priority: ServiceRequestPriority.Normal, requestedStartDate: null, requestedEndDate: null,
            locationCountryCode: "TR", locationCityCode: "IST", locationMarinaName: null,
            locationLatitude: null, locationLongitude: null, ownerNotes: null, expiresAt: null);
        sr.Publish();                       // → Open (biddable)
        db.ServiceRequests.Add(sr);
        await db.SaveChangesAsync();
        return sr.Id;                       // InMemory-generated id
    }

    private static async Task<uint> SeedDraftAsync(ServiceRequestDbContext db, long srId, string description)
    {
        var offer = ServiceRequestOfferEntity.Create(
            srId, ProviderProfileId, ProviderUserId, 0m, "TRY", description, null, null, null, null, null);
        db.ServiceRequestOffers.Add(offer);
        await db.SaveChangesAsync();
        return db.Entry(offer).Property<uint>("xmin").CurrentValue; // the persisted concurrency token
    }

    private static SaveOfferDraftRequest Req(string? token, string description) => new()
    {
        CurrencyCode = "TRY",
        Description = description,
        ConcurrencyToken = token,
        Items = new List<CreateServiceRequestOfferItemRequest>
        {
            new() { ItemType = ServiceRequestOfferItemType.Labor, Title = "Work", Quantity = 1, UnitPrice = 100m,
                    CurrencyCode = "TRY", TaxRate = 0.2m, PricingMethod = PricingMethod.Fixed },
        },
    };

    [Fact]
    public async Task Create_with_null_token_saves_and_returns_a_token()
    {
        var name = nameof(Create_with_null_token_saves_and_returns_a_token);
        await using var db = NewDb(name);
        var srId = await SeedBiddableSrAsync(db);
        db.ChangeTracker.Clear();

        var resp = await Handler(db).Handle(new SaveOfferDraftCommand(srId, Req(token: null, "v1")), default);

        resp.Should().NotBeNull();
        resp!.ConcurrencyToken.Should().NotBeNull("even a create returns the xmin token, never the offer id");
        resp.ConcurrencyToken.Should().NotBe(resp.Offer.Id.ToString(), "the token must not be the offer id");
    }

    [Fact]
    public async Task Existing_draft_null_token_saves_last_write_wins_compat()
    {
        var name = nameof(Existing_draft_null_token_saves_last_write_wins_compat);
        await using var db = NewDb(name);
        var srId = await SeedBiddableSrAsync(db);
        await SeedDraftAsync(db, srId, "original");
        db.ChangeTracker.Clear();

        var resp = await Handler(db).Handle(new SaveOfferDraftCommand(srId, Req(token: null, "updated")), default);

        resp!.Offer.Description.Should().Be("updated", "null token keeps last-write-wins for backward compatibility");
        resp.ConcurrencyToken.Should().NotBeNull();
    }

    [Fact]
    public async Task Existing_draft_matching_token_saves()
    {
        var name = nameof(Existing_draft_matching_token_saves);
        await using var db = NewDb(name);
        var srId = await SeedBiddableSrAsync(db);
        var xmin = await SeedDraftAsync(db, srId, "original");
        db.ChangeTracker.Clear();

        var resp = await Handler(db).Handle(new SaveOfferDraftCommand(srId, Req(token: xmin.ToString(), "updated")), default);

        resp!.Offer.Description.Should().Be("updated", "a token matching the persisted xmin saves");
        resp.ConcurrencyToken.Should().NotBeNull();
        resp.ConcurrencyToken.Should().NotBe(resp.Offer.Id.ToString(), "the token is the xmin, never the offer id");
    }

    [Fact]
    public async Task Existing_draft_stale_token_throws_and_does_not_overwrite()
    {
        var name = nameof(Existing_draft_stale_token_throws_and_does_not_overwrite);
        await using var db = NewDb(name);
        var srId = await SeedBiddableSrAsync(db);
        var xmin = await SeedDraftAsync(db, srId, "original");
        db.ChangeTracker.Clear();

        // A token that does NOT match the persisted xmin = a stale tab. The UPDATE's WHERE (xmin = staleToken) matches
        // 0 rows → DbUpdateConcurrencyException → SR_OFFER_STALE. The newer data must NOT be overwritten.
        var staleToken = (xmin + 1).ToString();
        var act = () => Handler(db).Handle(new SaveOfferDraftCommand(srId, Req(token: staleToken, "hijacked")), default);

        (await act.Should().ThrowAsync<AizenBusinessException>())
            .Which.Message.Should().Be("SR_OFFER_STALE");

        await using var verify = NewDb(name);
        var persisted = await verify.ServiceRequestOffers.AsNoTracking()
            .FirstAsync(o => o.ServiceRequestId == srId && o.ProviderProfileId == ProviderProfileId);
        persisted.Description.Should().Be("original", "a stale save must not overwrite the newer data");
    }

    [Fact]
    public async Task Existing_draft_unparsable_token_throws_stale_and_does_not_overwrite()
    {
        var name = nameof(Existing_draft_unparsable_token_throws_stale_and_does_not_overwrite);
        await using var db = NewDb(name);
        var srId = await SeedBiddableSrAsync(db);
        await SeedDraftAsync(db, srId, "original");
        db.ChangeTracker.Clear();

        // A legacy offer-id token like "1234" parses fine as uint, so use a genuinely non-numeric token to hit parse-fail.
        var act = () => Handler(db).Handle(new SaveOfferDraftCommand(srId, Req(token: "not-a-uint", "hijacked")), default);

        (await act.Should().ThrowAsync<AizenBusinessException>())
            .Which.Message.Should().Be("SR_OFFER_STALE", "an unparsable token is treated as stale, never as skip-the-check");

        // Nothing was persisted: a fresh context still shows the original draft, not the stale tab's overwrite.
        await using var verify = NewDb(name);
        var persisted = await verify.ServiceRequestOffers.AsNoTracking()
            .FirstAsync(o => o.ServiceRequestId == srId && o.ProviderProfileId == ProviderProfileId);
        persisted.Description.Should().Be("original", "the stale save must not overwrite the newer data");
    }
}
