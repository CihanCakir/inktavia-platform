using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Aizen.Modules.ServiceRequest.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

// FAZ 9 — Sınıf D regresyonu.
// ReferenceData birim-kodu araması ALTYAPI nedeniyle yapılamazsa (BaseUrl yok, 401, timeout, 5xx),
// doğrulama SESSİZCE atlanmamalı. Eskiden 'return null' → çağıran atlıyordu → geçersiz birim kodu
// "geçerli" sayılabiliyordu. Artık AizenUpstreamException (→502) fırlar; hata "birim doğrulaması
// yapılamadı" der, asla "kodunuz geçerli" demez.
public class UnitCodeValidatorTests
{
    private const string CacheKey = "refdata:measurement-units:active";

    private static UnitCodeValidator Build(
        IServiceRequestReferenceDataRemoteCall refData,
        IAizenDistributedCache cache)
        => new(refData, cache, NullLogger<UnitCodeValidator>.Instance);

    private static IEnumerable<CreateServiceRequestOfferItemRequest> Items(params string?[] unitCodes)
        => unitCodes.Select(c => new CreateServiceRequestOfferItemRequest { Title = "x", UnitCode = c });

    [Fact]
    public async Task RemoteCagriPatladiginda_UpstreamExceptionFirlatir_SessizceAtlamaz()
    {
        var cache = Substitute.For<IAizenDistributedCache>();
        cache.GetNoHash<List<string>>(CacheKey).Returns(Task.FromResult<List<string>>(null!)); // cache miss
        var refData = Substitute.For<IServiceRequestReferenceDataRemoteCall>();
        refData.GetActiveMeasurementUnits().Returns<Task<AizenApiResponse<List<SrMeasurementUnitDto>>>>(
            _ => throw new HttpRequestException("ReferenceData unreachable"));

        var sut = Build(refData, cache);

        var act = () => sut.ValidateUnitCodesAsync(Items("KG"), CancellationToken.None);

        await act.Should().ThrowAsync<AizenUpstreamException>(
            "altyapı hatası, sessizce atlanan bir doğrulama değil, 502 olarak yüzeye çıkmalı");
    }

    [Fact]
    public async Task TaninmayanKod_IsHatasiFirlatir()
    {
        // Cache hit ile geçerli küme sağlanır (remote'a gitmeden happy-path).
        var cache = Substitute.For<IAizenDistributedCache>();
        cache.GetNoHash<List<string>>(CacheKey).Returns(Task.FromResult(new List<string> { "KG", "M" }));
        var refData = Substitute.For<IServiceRequestReferenceDataRemoteCall>();

        var sut = Build(refData, cache);

        var act = () => sut.ValidateUnitCodesAsync(Items("TON"), CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>();
        await refData.DidNotReceive().GetActiveMeasurementUnits();
    }

    [Fact]
    public async Task TaninanKod_HataSuz_Gecer()
    {
        var cache = Substitute.For<IAizenDistributedCache>();
        cache.GetNoHash<List<string>>(CacheKey).Returns(Task.FromResult(new List<string> { "KG" }));
        var refData = Substitute.For<IServiceRequestReferenceDataRemoteCall>();

        var sut = Build(refData, cache);

        var act = () => sut.ValidateUnitCodesAsync(Items("kg"), CancellationToken.None); // case-insensitive

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BosVeyaKodsuzKalemler_RemoteCagriYapmadanGecer()
    {
        var cache = Substitute.For<IAizenDistributedCache>();
        var refData = Substitute.For<IServiceRequestReferenceDataRemoteCall>();

        var sut = Build(refData, cache);

        var act = () => sut.ValidateUnitCodesAsync(Items(null, "  "), CancellationToken.None);

        await act.Should().NotThrowAsync();
        await refData.DidNotReceive().GetActiveMeasurementUnits();
    }
}
