using Aizen.Modules.Identity.Domain.Entities.UserAgreement;
using FluentAssertions;

namespace Aizen.Modules.Identity.Domain.UnitTests.Entities;

public class AgreementEntityTests
{
    private static AgreementEntity CreateAgreement(
        string name = "Terms of Service",
        decimal initialVersion = 1.0m,
        string agreementType = "KVKK",
        bool isOptional = false)
    {
        return new AgreementEntity(name, initialVersion, agreementType, isOptional);
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var agreement = new AgreementEntity("Privacy Policy", 2.0m, "GDPR", true);

        agreement.Name.Should().Be("Privacy Policy");
        agreement.LastVersionNumber.Should().Be(2.0m);
        agreement.AgreementType.Should().Be("GDPR");
        agreement.IsOptional.Should().BeTrue();
        agreement.CreateDate.Should().NotBeNull();
        agreement.ModifyDate.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithIsOptionalFalse_ShouldSetIsOptionalFalse()
    {
        var agreement = CreateAgreement(isOptional: false);

        agreement.IsOptional.Should().BeFalse();
    }

    // ── UpdateMeta ────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateMeta_WithNewName_ShouldUpdateName()
    {
        var agreement = CreateAgreement(name: "Old Name");

        agreement.UpdateMeta("New Name", false);

        agreement.Name.Should().Be("New Name");
    }

    [Fact]
    public void UpdateMeta_WithChangedIsOptional_ShouldUpdateIsOptional()
    {
        var agreement = CreateAgreement(isOptional: false);

        agreement.UpdateMeta(agreement.Name, true);

        agreement.IsOptional.Should().BeTrue();
    }

    [Fact]
    public void UpdateMeta_WithSameValues_ShouldNotChangeFields()
    {
        var agreement = CreateAgreement(name: "Same Name", isOptional: true);

        agreement.UpdateMeta("Same Name", true);

        agreement.Name.Should().Be("Same Name");
        agreement.IsOptional.Should().BeTrue();
    }

    // ── BumpVersionIfHigher ───────────────────────────────────────────────────

    [Fact]
    public void BumpVersionIfHigher_WithHigherVersion_ShouldUpdateVersion()
    {
        var agreement = CreateAgreement(initialVersion: 1.0m);

        agreement.BumpVersionIfHigher(2.0m);

        agreement.LastVersionNumber.Should().Be(2.0m);
    }

    [Fact]
    public void BumpVersionIfHigher_WithLowerVersion_ShouldNotUpdateVersion()
    {
        var agreement = CreateAgreement(initialVersion: 3.0m);

        agreement.BumpVersionIfHigher(1.0m);

        agreement.LastVersionNumber.Should().Be(3.0m);
    }

    [Fact]
    public void BumpVersionIfHigher_WithEqualVersion_ShouldNotUpdateVersion()
    {
        var agreement = CreateAgreement(initialVersion: 2.0m);

        agreement.BumpVersionIfHigher(2.0m);

        agreement.LastVersionNumber.Should().Be(2.0m);
    }

    // ── IncrementVersion ─────────────────────────────────────────────────────

    [Fact]
    public void IncrementVersion_ShouldIncrementByOne()
    {
        var agreement = CreateAgreement(initialVersion: 1.0m);

        agreement.IncrementVersion();

        agreement.LastVersionNumber.Should().Be(2.0m);
    }

    [Fact]
    public void IncrementVersion_CalledTwice_ShouldIncrementByTwo()
    {
        var agreement = CreateAgreement(initialVersion: 1.0m);

        agreement.IncrementVersion();
        agreement.IncrementVersion();

        agreement.LastVersionNumber.Should().Be(3.0m);
    }

    // ── SetOptional ────────────────────────────────────────────────────────────

    [Fact]
    public void SetOptional_WithTrue_ShouldMarkAsOptional()
    {
        var agreement = CreateAgreement(isOptional: false);

        agreement.SetOptional(true);

        agreement.IsOptional.Should().BeTrue();
    }

    [Fact]
    public void SetOptional_WithFalse_ShouldMarkAsRequired()
    {
        var agreement = CreateAgreement(isOptional: true);

        agreement.SetOptional(false);

        agreement.IsOptional.Should().BeFalse();
    }

    // ── Rename ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Rename_ShouldUpdateName()
    {
        var agreement = CreateAgreement(name: "Old Name");

        agreement.Rename("New Agreement Name");

        agreement.Name.Should().Be("New Agreement Name");
    }
}
