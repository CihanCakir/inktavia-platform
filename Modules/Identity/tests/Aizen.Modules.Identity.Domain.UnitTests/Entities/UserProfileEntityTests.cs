using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Payment.Abstraction;
using FluentAssertions;

namespace Aizen.Modules.Identity.Domain.UnitTests.Entities;

public class UserProfileEntityTests
{
    private static UserProfileEntity CreateProfile(
        long userId = 1,
        string firstName = "John",
        string lastName = "Doe",
        TaxpayerType taxpayerType = TaxpayerType.Individual)
    {
        return UserProfileEntity.Create(userId, firstName, lastName, taxpayerType);
    }

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var birthDate = new DateTime(1990, 5, 10);
        var profile = UserProfileEntity.Create(
            userId: 42,
            firstName: "Jane",
            lastName: "Smith",
            taxpayerType: TaxpayerType.LLC,
            gender: "Female",
            birthDate: birthDate,
            bio: "A short bio",
            profilePhotoUrl: "https://example.com/photo.jpg");

        profile.UserId.Should().Be(42);
        profile.FirstName.Should().Be("Jane");
        profile.LastName.Should().Be("Smith");
        profile.TaxpayerType.Should().Be(TaxpayerType.LLC);
        profile.Gender.Should().Be("Female");
        profile.BirthDate.Should().Be(birthDate);
        profile.Bio.Should().Be("A short bio");
        profile.ProfilePhotoUrl.Should().Be("https://example.com/photo.jpg");
        profile.ApprovalStatus.Should().Be(ApprovalStatus.Pending);
        profile.CreateDate.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithDefaultOptionals_ShouldHaveNullOptionalFields()
    {
        var profile = CreateProfile();

        profile.Gender.Should().BeNull();
        profile.BirthDate.Should().BeNull();
        profile.Bio.Should().BeNull();
        profile.ProfilePhotoUrl.Should().BeNull();
    }

    // ── Approve ─────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_WhenPending_ShouldSetApprovedStatus()
    {
        var profile = CreateProfile();

        profile.Approve();

        profile.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
        profile.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ShouldBeIdempotent()
    {
        var profile = CreateProfile();
        profile.Approve();

        var act = () => profile.Approve();

        act.Should().NotThrow();
        profile.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
    }

    [Fact]
    public void Approve_WhenAlreadyRejected_ShouldThrowAizenBusinessException()
    {
        var profile = CreateProfile();
        profile.Reject("Invalid documents");

        var act = () => profile.Approve();

        act.Should().Throw<AizenBusinessException>();
    }

    // ── Reject ──────────────────────────────────────────────────────────────

    [Fact]
    public void Reject_WhenPending_ShouldSetRejectedStatus()
    {
        var profile = CreateProfile();

        profile.Reject("Missing tax ID");

        profile.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);
        profile.RejectReason.Should().Be("Missing tax ID");
        profile.RejectedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ShouldBeIdempotent()
    {
        var profile = CreateProfile();
        profile.Reject("Original reason");

        var act = () => profile.Reject("New reason");

        act.Should().NotThrow();
        profile.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);
    }

    [Fact]
    public void Reject_WhenAlreadyApproved_ShouldThrowAizenBusinessException()
    {
        var profile = CreateProfile();
        profile.Approve();

        var act = () => profile.Reject("Some reason");

        act.Should().Throw<AizenBusinessException>();
    }

    [Fact]
    public void Reject_WithEmptyReason_ShouldThrowAizenBusinessException()
    {
        var profile = CreateProfile();

        var act = () => profile.Reject("");

        act.Should().Throw<AizenBusinessException>();
    }

    [Fact]
    public void Reject_WithWhitespaceReason_ShouldThrowAizenBusinessException()
    {
        var profile = CreateProfile();

        var act = () => profile.Reject("   ");

        act.Should().Throw<AizenBusinessException>();
    }

    // ── ChangeName ──────────────────────────────────────────────────────────

    [Fact]
    public void ChangeName_ShouldUpdateFirstAndLastName()
    {
        var profile = CreateProfile(firstName: "Old", lastName: "Name");

        profile.ChangeName("New", "Name2");

        profile.FirstName.Should().Be("New");
        profile.LastName.Should().Be("Name2");
    }

    // ── UpdateBio ───────────────────────────────────────────────────────────

    [Fact]
    public void UpdateBio_ShouldSetBio()
    {
        var profile = CreateProfile();

        profile.UpdateBio("Updated bio text");

        profile.Bio.Should().Be("Updated bio text");
    }

    [Fact]
    public void UpdateBio_WithNull_ShouldClearBio()
    {
        var profile = UserProfileEntity.Create(1, "A", "B", TaxpayerType.Individual, bio: "existing bio");

        profile.UpdateBio(null);

        profile.Bio.Should().BeNull();
    }

    // ── UpdateProfilePhoto ───────────────────────────────────────────────────

    [Fact]
    public void UpdateProfilePhoto_ShouldSetPhotoUrl()
    {
        var profile = CreateProfile();

        profile.UpdateProfilePhoto("https://cdn.example.com/new.png");

        profile.ProfilePhotoUrl.Should().Be("https://cdn.example.com/new.png");
    }

    // ── UpdateGender ─────────────────────────────────────────────────────────

    [Fact]
    public void UpdateGender_ShouldSetGender()
    {
        var profile = CreateProfile();

        profile.UpdateGender("Male");

        profile.Gender.Should().Be("Male");
    }

    // ── UpdateBirthDate ───────────────────────────────────────────────────────

    [Fact]
    public void UpdateBirthDate_ShouldSetBirthDate()
    {
        var profile = CreateProfile();
        var date = new DateTime(2000, 1, 1);

        profile.UpdateBirthDate(date);

        profile.BirthDate.Should().Be(date);
    }

    // ── UpdateTaxpayerType ────────────────────────────────────────────────────

    [Fact]
    public void UpdateTaxpayerType_WhenDifferent_ShouldUpdateType()
    {
        var profile = CreateProfile(taxpayerType: TaxpayerType.Individual);

        profile.UpdateTaxpayerType(TaxpayerType.Corporation);

        profile.TaxpayerType.Should().Be(TaxpayerType.Corporation);
    }

    [Fact]
    public void UpdateTaxpayerType_WhenSame_ShouldRemainUnchanged()
    {
        var profile = CreateProfile(taxpayerType: TaxpayerType.Individual);

        profile.UpdateTaxpayerType(TaxpayerType.Individual);

        profile.TaxpayerType.Should().Be(TaxpayerType.Individual);
    }
}
