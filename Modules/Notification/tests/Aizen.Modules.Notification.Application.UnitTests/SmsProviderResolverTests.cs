using Aizen.Modules.Notification.Application.Services;
using FluentAssertions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>Faz 28.7 — sağlayıcı seçimi: sır dolu → gerçek adaptör; boş/placeholder → stub'a düşer (config ile seçim).</summary>
public sealed class SmsProviderResolverTests
{
    [Fact]
    public void Default_stub()
        => SmsProviderResolver.Resolve(new SmsOptions { Provider = "stub" }).Should().Be(SmsProviderKind.Stub);

    [Fact]
    public void Infobip_with_apikey_selects_infobip()
        => SmsProviderResolver.Resolve(new SmsOptions
        {
            Provider = "infobip",
            Infobip = new InfobipSmsOptions { ApiKey = "KEY" },
        }).Should().Be(SmsProviderKind.Infobip);

    [Fact]
    public void Provider_is_case_insensitive()
        => SmsProviderResolver.Resolve(new SmsOptions
        {
            Provider = "INFOBIP",
            Infobip = new InfobipSmsOptions { ApiKey = "KEY" },
        }).Should().Be(SmsProviderKind.Infobip);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("__FROM_ENV__")]
    [InlineData("__FROM_SECRET__")]
    public void Infobip_with_empty_or_placeholder_apikey_falls_back_to_stub(string apiKey)
        => SmsProviderResolver.Resolve(new SmsOptions
        {
            Provider = "infobip",
            Infobip = new InfobipSmsOptions { ApiKey = apiKey },
        }).Should().Be(SmsProviderKind.Stub);

    [Fact]
    public void Netgsm_with_usercode_and_password_selects_netgsm()
        => SmsProviderResolver.Resolve(new SmsOptions
        {
            Provider = "netgsm",
            Netgsm = new NetgsmSmsOptions { UserCode = "u", Password = "p" },
        }).Should().Be(SmsProviderKind.Netgsm);

    [Fact]
    public void Netgsm_missing_password_falls_back_to_stub()
        => SmsProviderResolver.Resolve(new SmsOptions
        {
            Provider = "netgsm",
            Netgsm = new NetgsmSmsOptions { UserCode = "u", Password = "__FROM_SECRET__" },
        }).Should().Be(SmsProviderKind.Stub);
}
