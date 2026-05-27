using Aizen.Modules.InktaviaStore.Application.Identity.Command.CheckOtp;
using FluentAssertions;

namespace Aizen.Modules.Identity.Application.UnitTests.Validators;

public class CheckOtpCommandValidatorTests
{
    private readonly CheckOtpCommandValidator _validator = new();

    private static CheckOtpCommand ValidCommand() =>
        new("0551234567", 123456, "some-guid-value");

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullPhoneNumber_ShouldFail()
    {
        var cmd = new CheckOtpCommand(null!, 123456, "some-guid");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public void Validate_WithEmptyPhoneNumber_ShouldFail()
    {
        var cmd = new CheckOtpCommand("", 123456, "some-guid");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public void Validate_WithShortPhoneNumber_ShouldFail()
    {
        var cmd = new CheckOtpCommand("05512345", 123456, "some-guid");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public void Validate_WithLongPhoneNumber_ShouldFail()
    {
        var cmd = new CheckOtpCommand("055123456789", 123456, "some-guid");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public void Validate_WithOtpTooLow_ShouldFail()
    {
        var cmd = new CheckOtpCommand("0551234567", 99999, "some-guid");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Otp");
    }

    [Fact]
    public void Validate_WithOtpTooHigh_ShouldFail()
    {
        var cmd = new CheckOtpCommand("0551234567", 1000000, "some-guid");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Otp");
    }

    [Fact]
    public void Validate_WithNullValidationGuid_ShouldFail()
    {
        var cmd = new CheckOtpCommand("0551234567", 123456, null!);

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ValidationGuid");
    }

    [Fact]
    public void Validate_WithEmptyValidationGuid_ShouldFail()
    {
        var cmd = new CheckOtpCommand("0551234567", 123456, "");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ValidationGuid");
    }
}
