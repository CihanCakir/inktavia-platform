using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.InktaviaStore.Application.Identity.Command;
using FluentAssertions;

namespace Aizen.Modules.Identity.Application.UnitTests.Validators;

public class LoginWithPhoneNumberValidatorTests
{
    private readonly LoginWithPhoneNumberValidator _validator = new();

    private static LoginWithPhoneNumberCommand ValidCommand() =>
        new("+905551234567", "Password1!", "device-id-abc", "notification-token");

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyPhoneNumber_ShouldFail()
    {
        var cmd = new LoginWithPhoneNumberCommand("", "Password1!", "device-id", "token");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public void Validate_WithPhoneNotStartingWithPlus90_ShouldFail()
    {
        var cmd = new LoginWithPhoneNumberCommand("05551234567", "Password1!", "device-id", "token");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public void Validate_WithShortPhone_ShouldFail()
    {
        var cmd = new LoginWithPhoneNumberCommand("+9055512345", "Password1!", "device-id", "token");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public void Validate_WithEmptyPassword_ShouldFail()
    {
        var cmd = new LoginWithPhoneNumberCommand("+905551234567", "", "device-id", "token");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_WithEmptyDeviceId_ShouldFail()
    {
        var cmd = new LoginWithPhoneNumberCommand("+905551234567", "Password1!", "", "token");

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DeviceId");
    }
}
