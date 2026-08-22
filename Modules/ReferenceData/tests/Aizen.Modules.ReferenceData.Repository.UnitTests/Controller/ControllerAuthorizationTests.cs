using System.Reflection;
using Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace Aizen.Modules.ReferenceData.Repository.UnitTests.Controller;

// FAZ 9 — Sınıf B regresyonu.
// AddAizenKeycloakAuth global bir FallbackPolicy = RequireAuthenticatedUser() kaydeder; bu yüzden
// attribute'suz ("örtük") bir controller da auth ister. Token'sız modül→modül çağıranı OLAN salt-okunur
// referans uçları [AllowAnonymous] taşımalı, aksi halde çağrı 401 olur. Bu test o attribute'ların
// yanlışlıkla kaldırılmasına karşı koruma sağlar.
public class ControllerAuthorizationTests
{
    // ExchangeRate ve Lookup, ServiceRequest modülünden token'sız çağrılıyor
    // (IServiceRequestReferenceDataRemoteCall Authorization iletmez) → [AllowAnonymous] ŞART.
    [Theory]
    [InlineData(typeof(ExchangeRateController))]
    [InlineData(typeof(LookupController))]
    public void TokensizCagrilanReferansControllerlari_AllowAnonymousTasimali(Type controllerType)
    {
        controllerType.GetCustomAttribute<AllowAnonymousAttribute>()
            .Should()
            .NotBeNull(
                $"{controllerType.Name} token'sız modül→modül çağrılıyor; [AllowAnonymous] " +
                "kaldırılırsa global FallbackPolicy nedeniyle 401 olur");
    }

    // Currency'yi yalnız AdminPanel BFF çağırıyor (servis token'ını iletir); niyet AÇIK [Authorize] olmalı,
    // örtük fallback'e bırakılmamalı ve kesinlikle anonim OLMAMALI.
    [Fact]
    public void CurrencyController_AcikAuthorizeTasimali_AnonimOlmamali()
    {
        typeof(CurrencyController).GetCustomAttribute<AuthorizeAttribute>()
            .Should().NotBeNull("CurrencyController açık [Authorize] taşımalı");
        typeof(CurrencyController).GetCustomAttribute<AllowAnonymousAttribute>()
            .Should().BeNull("CurrencyController anonim OLMAMALI");
    }
}
