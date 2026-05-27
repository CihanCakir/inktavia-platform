using Aizen.Modules.Identity.Abstraction.Enum;
using Aizen.Modules.Identity.Domain.Entities;
using FluentAssertions;

namespace Aizen.Modules.Identity.Domain.UnitTests.Entities;

public class UserValidationEntityTests
{
    private static UserValidationEntity CreateValid(
        DateTime? expireDate = null,
        int code = 123456,
        string guid = "test-guid-1234")
    {
        return UserValidationEntity.Create(
            validationType: UserValidationType.Login,
            methodType: UserValidationMethodType.Sms,
            reference: "+905551234567",
            code: code,
            guid: guid,
            expireDate: expireDate ?? DateTime.UtcNow.AddMinutes(5),
            applicationId: 1,
            userProfileId: 10);
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var expireDate = DateTime.UtcNow.AddMinutes(10);
        var entity = UserValidationEntity.Create(
            validationType: UserValidationType.Register,
            methodType: UserValidationMethodType.Email,
            reference: "user@example.com",
            code: 654321,
            guid: "unique-guid",
            expireDate: expireDate,
            applicationId: 2,
            userProfileId: 99,
            relationCode: "REL-001");

        entity.UserValidationTypeId.Should().Be(UserValidationType.Register);
        entity.UserValidationMethodTypeId.Should().Be(UserValidationMethodType.Email);
        entity.ValidationReferance.Should().Be("user@example.com");
        entity.ValdationCode.Should().Be(654321);
        entity.ValidationGuid.Should().Be("unique-guid");
        entity.ExpiredDateTime.Should().Be(expireDate);
        entity.ApplicationId.Should().Be(2);
        entity.UserProfileId.Should().Be(99);
        entity.RelationCode.Should().Be("REL-001");
        entity.State.Should().Be(1);
    }

    [Fact]
    public void Create_ShouldHaveInitialStateOfOne()
    {
        var entity = CreateValid();

        entity.State.Should().Be(1);
    }

    [Fact]
    public void Create_WithoutRelationCode_ShouldBeNull()
    {
        var entity = CreateValid();

        entity.RelationCode.Should().BeNull();
    }

    // ── MarkAsUsed ───────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsUsed_ShouldSetStateToTwo()
    {
        var entity = CreateValid();

        entity.MarkAsUsed();

        entity.State.Should().Be(2);
    }

    // ── Expire ───────────────────────────────────────────────────────────────

    [Fact]
    public void Expire_ShouldSetStateToThree()
    {
        var entity = CreateValid();

        entity.Expire();

        entity.State.Should().Be(3);
    }

    // ── IsExpired ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsExpired_WhenExpireDateIsInFuture_ShouldReturnFalse()
    {
        var entity = CreateValid(expireDate: DateTime.UtcNow.AddMinutes(10));

        entity.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpireDateIsInPast_ShouldReturnTrue()
    {
        var entity = CreateValid(expireDate: DateTime.UtcNow.AddMinutes(-1));

        entity.IsExpired().Should().BeTrue();
    }

    // ── IsValid ───────────────────────────────────────────────────────────────

    [Fact]
    public void IsValid_WithCorrectCodeAndGuid_ShouldReturnTrue()
    {
        var entity = CreateValid(code: 111111, guid: "correct-guid");

        entity.IsValid(111111, "correct-guid").Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithWrongCode_ShouldReturnFalse()
    {
        var entity = CreateValid(code: 111111, guid: "correct-guid");

        entity.IsValid(999999, "correct-guid").Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithWrongGuid_ShouldReturnFalse()
    {
        var entity = CreateValid(code: 111111, guid: "correct-guid");

        entity.IsValid(111111, "wrong-guid").Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var entity = CreateValid(
            expireDate: DateTime.UtcNow.AddMinutes(-1),
            code: 111111,
            guid: "correct-guid");

        entity.IsValid(111111, "correct-guid").Should().BeFalse();
    }

    [Fact]
    public void IsValid_AfterMarkAsUsed_ShouldReturnFalse()
    {
        var entity = CreateValid(code: 111111, guid: "correct-guid");
        entity.MarkAsUsed();

        entity.IsValid(111111, "correct-guid").Should().BeFalse();
    }

    [Fact]
    public void IsValid_AfterExpire_ShouldReturnFalse()
    {
        var entity = CreateValid(code: 111111, guid: "correct-guid");
        entity.Expire();

        entity.IsValid(111111, "correct-guid").Should().BeFalse();
    }
}
