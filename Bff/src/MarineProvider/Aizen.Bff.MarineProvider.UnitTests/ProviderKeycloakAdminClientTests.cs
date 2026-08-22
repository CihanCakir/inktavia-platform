using System.Net;
using System.Text.Json.Nodes;
using Aizen.Bff.MarineProvider.Application.Common.Options;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.MarineProvider.UnitTests;

// FAZ10 #38 regresyonu.
// SetUserAttributeAsync eskiden GET yanıtını minimal tipli UserRepresentation'a deserialize edip PUT ediyordu;
// Keycloak PUT temsili DEĞİŞTİRDİĞİ için tipte modellenmemiş her alan (requiredActions, federatedIdentities,
// createdTimestamp ...) her yazımda siliniyordu. 2026-08-21'de bu bir kullanıcının profil alanlarını bozup
// VERIFY_PROFILE'ı tetikledi ve girişi kilitledi. Bu test, GET'te var olan ama tipte MODELLENMEMİŞ bir alanın
// (requiredActions) PUT gövdesinde AYNEN korunduğunu — ve tek attribute'un doğru yazıldığını — kanıtlar.
public class ProviderKeycloakAdminClientTests
{
    [Fact]
    public async Task SetUserAttributeAsync_UnmodeledFieldsSurvivePut()
    {
        const string userJson = """
        {
          "id": "u1",
          "username": "ada@example.com",
          "email": "ada@example.com",
          "firstName": "Ada",
          "lastName": "Lovelace",
          "enabled": true,
          "emailVerified": true,
          "requiredActions": ["VERIFY_PROFILE"],
          "federatedIdentities": [{ "identityProvider": "google", "userId": "g-1" }],
          "createdTimestamp": 1710000000000,
          "attributes": { "existing_attr": ["keep-me"] }
        }
        """;

        var handler = new CapturingHandler(userJson);
        var options = Options.Create(new MarineProviderKeycloakOptions
        {
            BaseUrl = "http://keycloak:8080",
            Realm = "inktavia-realm"
        });
        var sut = new ProviderKeycloakAdminClient(
            new StubHttpClientFactory(handler),
            new StubTokenProvider(),
            options);

        await sut.SetUserAttributeAsync("u1", "provider_profile_id", "42", CancellationToken.None);

        handler.CapturedPutBody.Should().NotBeNull("bir PUT yapılmış olmalı");
        var put = JsonNode.Parse(handler.CapturedPutBody!)!.AsObject();

        // 1) Tipte MODELLENMEMİŞ alanlar korundu:
        ((string?)put["requiredActions"]?[0]).Should().Be("VERIFY_PROFILE");
        ((string?)put["federatedIdentities"]?[0]?["identityProvider"]).Should().Be("google");
        ((long?)put["createdTimestamp"]).Should().Be(1710000000000);

        // 2) Yönetilen kimlik alanları korundu (2026-08-21 olayında silinenler):
        ((string?)put["email"]).Should().Be("ada@example.com");
        ((string?)put["firstName"]).Should().Be("Ada");
        ((string?)put["lastName"]).Should().Be("Lovelace");

        // 3) Var olan diğer attribute korundu ve hedef attribute doğru yazıldı:
        ((string?)put["attributes"]?["existing_attr"]?[0]).Should().Be("keep-me");
        ((string?)put["attributes"]?["provider_profile_id"]?[0]).Should().Be("42");
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _getBody;
        public string? CapturedPutBody { get; private set; }

        public CapturingHandler(string getBody) => _getBody = getBody;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_getBody, System.Text.Encoding.UTF8, "application/json")
                };

            if (request.Method == HttpMethod.Put)
            {
                CapturedPutBody = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        // Mutlak URL kullanıldığı için BaseAddress gerekmez.
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class StubTokenProvider : IProviderKeycloakServiceTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
            => Task.FromResult("test-token");
    }
}
