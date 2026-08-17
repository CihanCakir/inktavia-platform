using System.Reflection;
using Aizen.Bff.Marine.Web.Application.Contact.Command.SubmitWebContact;
using Aizen.Modules.Notification.Abstraction.Response;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// M4 — the BFF contact submit forwards the untrusted payload (incl. honeypot/captcha) to Notification, forwards the
/// caller IP as X-Forwarded-For, and returns only { accepted, ticketRef }. Shape-validated by
/// <see cref="SubmitWebContactCommandValidator"/>.
/// </summary>
public sealed class WebContactTests
{
    private static SubmitWebContactCommandHandler Handler(FakeNotificationRemoteCall notification)
        => new(notification, NullLogger<SubmitWebContactCommandHandler>.Instance);

    [Fact]
    public async Task Forwards_payload_and_ip_and_returns_ticketRef()
    {
        var notification = new FakeNotificationRemoteCall();

        var resp = await Handler(notification).Handle(new SubmitWebContactCommand
        {
            Name = "Ada", Email = "ada@example.com", Subject = "Berths", Message = "Hi there.",
            SourcePage = "/contact", Honeypot = "", CaptchaToken = "tok", ClientIp = "203.0.113.9",
        }, default);

        resp!.Accepted.Should().BeTrue();
        resp.TicketRef.Should().Be("CT-TEST123456");
        notification.LastForwardedFor.Should().Be("203.0.113.9", "the caller IP is forwarded for the module to hash");
        notification.LastRequest!.Name.Should().Be("Ada");
        notification.LastRequest.Honeypot.Should().Be("");     // honeypot passed through untrusted
        notification.LastRequest.CaptchaToken.Should().Be("tok");
    }

    [Theory]
    [InlineData("", "a@b.com", "s", "message")]         // missing name
    [InlineData("Ada", "not-an-email", "s", "message")]  // bad email
    [InlineData("Ada", "a@b.com", "", "message")]        // missing subject
    [InlineData("Ada", "a@b.com", "s", "")]              // missing message
    public void Validator_rejects_malformed_payloads(string name, string email, string subject, string message)
    {
        var result = new SubmitWebContactCommandValidator().Validate(new SubmitWebContactCommand
        { Name = name, Email = email, Subject = subject, Message = message });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_accepts_a_well_formed_payload()
        => new SubmitWebContactCommandValidator().Validate(new SubmitWebContactCommand
        { Name = "Ada", Email = "ada@example.com", Subject = "Berths", Message = "Hello there." })
            .IsValid.Should().BeTrue();

    [Fact]
    public void Response_carries_only_accepted_and_ticketRef()
        => typeof(SubmitContactResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).Should().BeEquivalentTo("Accepted", "TicketRef");

    [Fact]
    public void Contact_endpoint_is_on_the_stricter_contact_submit_policy()
    {
        var method = typeof(Aizen.Bff.Marine.Web.Controllers.V1.ContactController).GetMethod("Submit")!;
        var policy = method.GetCustomAttribute<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>();
        policy.Should().NotBeNull();
        policy!.PolicyName.Should().Be("contact-submit", "the write surface must not share the public-read-ip limit");
    }
}
