using System.Net;
using Aizen.Modules.Notification.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>Faz 28.7 — Netgsm adaptörü: yanıt-kodu eşlemesi ("00"→jobid; 30/40 hata) ve Basic auth + '+'siz numara.</summary>
public sealed class NetgsmSmsSenderTests
{
    private static NetgsmSmsSender Sut(FakeHttpMessageHandler handler)
        => new(new StubHttpClientFactory(handler),
               Options.Create(new SmsOptions
               {
                   Provider = "netgsm",
                   Netgsm = new NetgsmSmsOptions { UserCode = "user1", Password = "pass1", MsgHeader = "INKTAVIA" },
               }),
               NullLogger<NetgsmSmsSender>.Instance);

    [Fact]
    public async Task Code_00_maps_to_success_with_jobid_and_basic_auth()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"code\":\"00\",\"jobid\":\"JOB-9\",\"description\":\"OK\"}");

        var result = await Sut(handler).SendAsync("+905551234567", "Merhaba", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ProviderRef.Should().Be("JOB-9");
        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/sms/rest/v2/send");
        handler.LastRequest.Headers.GetValues("Authorization").Should().ContainSingle().Which.Should().StartWith("Basic ");
        // Numara '+' olmadan gönderilir.
        handler.LastRequestBody.Should().Contain("905551234567").And.NotContain("+90");
    }

    [Fact]
    public async Task Code_30_maps_to_credential_failure()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"code\":\"30\"}");

        var result = await Sut(handler).SendAsync("+905551234567", "Merhaba", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("kullanıcı adı/şifre");
    }

    [Fact]
    public async Task Code_40_maps_to_unapproved_header_failure()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"code\":\"40\",\"description\":\"baslik onaysiz\"}");

        var result = await Sut(handler).SendAsync("+905551234567", "Merhaba", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("onaysız");          // "mesaj başlığı (sender id) onaysız"
        result.Error.Should().Contain("baslik onaysiz");   // description eklenir
    }

    [Fact]
    public async Task Unknown_code_maps_to_generic_failure()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"code\":\"99\"}");

        var result = await Sut(handler).SendAsync("+905551234567", "Merhaba", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("99");
    }

    [Fact]
    public async Task Non_2xx_returns_failure()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "oops");

        var result = await Sut(handler).SendAsync("+905551234567", "Merhaba", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("500");
    }
}
