using System.Reflection;
using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Newsletter.Command.SubscribeWebNewsletter;
using Aizen.Bff.Marine.Web.Filters;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Response;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// P1 — the BFF newsletter surface is an anonymous WRITE: [AllowAnonymous], on the stricter <c>newsletter-submit</c>
/// rate-limit policy, with NO cache attribute. The subscribe handler forwards the caller IP unchanged as
/// X-Forwarded-For, and every response binds through the <see cref="AizenApiResponse{T}"/> envelope.
/// </summary>
public sealed class WebNewsletterTests
{
    private static readonly Type Controller = typeof(Aizen.Bff.Marine.Web.Controllers.V1.NewsletterController);

    private static SubscribeWebNewsletterCommandHandler Handler(FakeNotificationRemoteCall n)
        => new(n, NullLogger<SubscribeWebNewsletterCommandHandler>.Instance);

    // 13
    [Fact]
    public void Controller_is_anonymous_rate_limited_and_uncached()
    {
        Controller.GetCustomAttribute<AllowAnonymousAttribute>()
            .Should().NotBeNull("the public newsletter surface is anonymous");

        foreach (var name in new[] { "Subscribe", "Confirm", "Unsubscribe" })
        {
            var method = Controller.GetMethod(name)!;
            var policy = method.GetCustomAttribute<EnableRateLimitingAttribute>();
            policy.Should().NotBeNull($"{name} must be rate limited");
            policy!.PolicyName.Should().Be("newsletter-submit", $"{name} must not share public-read-ip");

            method.GetCustomAttribute<WebCacheAttribute>()
                .Should().BeNull($"{name} is a write — it must not be cached");
        }

        Controller.GetCustomAttribute<WebCacheAttribute>()
            .Should().BeNull("the controller must not carry a cache attribute");
    }

    // 14
    [Fact]
    public async Task Subscribe_handler_forwards_x_forwarded_for_unchanged()
    {
        var n = new FakeNotificationRemoteCall();

        await Handler(n).Handle(new SubscribeWebNewsletterCommand
        {
            Email = "ada@example.com", Lang = "tr", SourcePage = "/tr", ClientIp = "203.0.113.9",
        }, default);

        n.LastSubscribeForwardedFor.Should().Be("203.0.113.9", "the caller IP is forwarded for the module to hash");
        n.LastSubscribeRequest!.Email.Should().Be("ada@example.com");
        n.LastSubscribeRequest.Lang.Should().Be("tr");
    }

    // 15
    [Fact]
    public void Newsletter_remote_calls_bind_the_envelope()
    {
        var subscribe = typeof(INotificationRemoteCall).GetMethod(nameof(INotificationRemoteCall.SubscribeNewsletter))!;
        subscribe.ReturnType.Should().Be(typeof(Task<AizenApiResponse<NewsletterSubscribeResponse>>));

        var confirm = typeof(INotificationRemoteCall).GetMethod(nameof(INotificationRemoteCall.ConfirmNewsletter))!;
        confirm.ReturnType.Should().Be(typeof(Task<AizenApiResponse<NewsletterConfirmResponse>>));

        var unsubscribe = typeof(INotificationRemoteCall).GetMethod(nameof(INotificationRemoteCall.UnsubscribeNewsletter))!;
        unsubscribe.ReturnType.Should().Be(typeof(Task<AizenApiResponse<NewsletterUnsubscribeResponse>>));
    }

    [Fact]
    public async Task Subscribe_handler_unwraps_envelope_body()
    {
        var n = new FakeNotificationRemoteCall
        {
            SubscribeResponse = new AizenApiResponse<NewsletterSubscribeResponse>
            {
                Body = new NewsletterSubscribeResponse { Accepted = true },
            },
        };

        var resp = await Handler(n).Handle(new SubscribeWebNewsletterCommand
        {
            Email = "ada@example.com", Lang = "en",
        }, default);

        resp!.Accepted.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "tr")]                 // missing email
    [InlineData("not-an-email", "tr")]     // bad email
    [InlineData("a@b.com", "")]            // missing lang
    public void Validator_rejects_malformed_payloads(string email, string lang)
        => new SubscribeWebNewsletterCommandValidator()
            .Validate(new SubscribeWebNewsletterCommand { Email = email, Lang = lang })
            .IsValid.Should().BeFalse();
}
