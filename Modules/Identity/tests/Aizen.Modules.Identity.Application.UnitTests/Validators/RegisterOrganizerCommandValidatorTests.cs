using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.RegisterOrganizer;
using FluentAssertions;

namespace Aizen.Modules.Identity.Application.UnitTests.Validators;

public class RegisterOrganizerCommandValidatorTests
{
    private readonly RegisterOrganizerCommandValidator _validator = new();

    private static RegisterOrganizerCommand ValidCommand() =>
        new(
            email: "organizer@example.com",
            password: "Secret123",
            companyName: "Acme Corp",
            taxNo: "1234567890",
            contactPhone: "+905551234567",
            ownerFirstName: "Alice",
            ownerLastName: "Smith",
            kvkkAccepted: true,
            deviceId: null,
            deviceType: null,
            notificationToken: null);

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyEmail_ShouldFail()
    {
        var cmd = new RegisterOrganizerCommand(
            "", "Secret123", "Acme", "123", "+905551234567",
            "Alice", "Smith", true, null, null, null);

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithInvalidEmail_ShouldFail()
    {
        var cmd = new RegisterOrganizerCommand(
            "not-an-email", "Secret123", "Acme", "123", "+905551234567",
            "Alice", "Smith", true, null, null, null);

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithShortPassword_ShouldFail()
    {
        var cmd = new RegisterOrganizerCommand(
            "organizer@example.com", "Ab1", "Acme", "123", "+905551234567",
            "Alice", "Smith", true, null, null, null);

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_WithEmptyOwnerFirstName_ShouldFail()
    {
        var cmd = new RegisterOrganizerCommand(
            "organizer@example.com", "Secret123", "Acme", "123", "+905551234567",
            "", "Smith", true, null, null, null);

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OwnerFirstName");
    }

    [Fact]
    public void Validate_WithEmptyOwnerLastName_ShouldFail()
    {
        var cmd = new RegisterOrganizerCommand(
            "organizer@example.com", "Secret123", "Acme", "123", "+905551234567",
            "Alice", "", true, null, null, null);

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OwnerLastName");
    }

    [Fact]
    public void Validate_WithKvkkNotAccepted_ShouldFail()
    {
        var cmd = new RegisterOrganizerCommand(
            "organizer@example.com", "Secret123", "Acme", "123", "+905551234567",
            "Alice", "Smith", false, null, null, null);

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "KvkkAccepted");
    }

    [Fact]
    public void Validate_WithTooLongDeviceId_ShouldFail()
    {
        var tooLong = new string('X', 201);
        var cmd = new RegisterOrganizerCommand(
            "organizer@example.com", "Secret123", "Acme", "123", "+905551234567",
            "Alice", "Smith", true, tooLong, null, null);

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DeviceId");
    }
}
