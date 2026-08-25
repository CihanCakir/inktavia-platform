using System.Net;
using Aizen.Modules.Notification.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>Faz 28.7 — Infobip adaptörü: mutlu yol messageId'yi ProviderRef yapar; non-2xx → başarısızlık (gövde özetli).</summary>
public sealed class InfobipSmsSenderTests
{
    private static InfobipSmsSender Sut(FakeHttpMessageHandler handler)
        => new(new StubHttpClientFactory(handler),
               Options.Create(new SmsOptions
               {
                   Provider = "infobip",
                   Infobip = new InfobipSmsOptions { BaseUrl = "https://x.api.infobip.com", ApiKey = "KEY-123", From = "Inktavia" },
               }),
               NullLogger<InfobipSmsSender>.Instance);

    [Fact]
    public async Task Happy_path_parses_messageId_and_sends_expected_request()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            "{\"messages\":[{\"messageId\":\"MSG-123\",\"status\":{\"groupId\":1,\"groupName\":\"PENDING\",\"description\":\"sent\"}}]}");

        var result = await Sut(handler).SendAsync("+905551234567", "Merhaba", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ProviderRef.Should().Be("MSG-123");
        // İstek doğrulaması: advanced endpoint, "App {ApiKey}" auth, gövdede hedef numara + from.
        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/sms/2/text/advanced");
        handler.LastRequest.Headers.GetValues("Authorization").Should().ContainSingle().Which.Should().Be("App KEY-123");
        // Not: System.Text.Json '+' karakterini + olarak kaçırır; rakamları kontrol etmek yeterli.
        handler.LastRequestBody.Should().Contain("905551234567").And.Contain("Inktavia");
    }

    [Fact]
    public async Task Non_2xx_returns_failure_with_status_in_error()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest,
            "{\"requestError\":{\"serviceException\":{\"messageId\":\"BAD_REQUEST\",\"text\":\"invalid\"}}}");

        var result = await Sut(handler).SendAsync("+905551234567", "Merhaba", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("400");
    }

    [Fact]
    public async Task Rejected_status_returns_failure()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            "{\"messages\":[{\"messageId\":\"MSG-9\",\"status\":{\"groupId\":5,\"groupName\":\"REJECTED\",\"description\":\"blacklisted\"}}]}");

        var result = await Sut(handler).SendAsync("+905551234567", "Merhaba", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("REJECTED");
    }

    [Fact]
    public async Task Missing_config_returns_failure_without_calling_http()
    {
        var sut = new InfobipSmsSender(
            new StubHttpClientFactory(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}")),
            Options.Create(new SmsOptions { Provider = "infobip", Infobip = new InfobipSmsOptions { ApiKey = "__FROM_ENV__" } }),
            NullLogger<InfobipSmsSender>.Instance);

        var result = await sut.SendAsync("+905551234567", "Merhaba", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("yapılandırması eksik");
    }
}
