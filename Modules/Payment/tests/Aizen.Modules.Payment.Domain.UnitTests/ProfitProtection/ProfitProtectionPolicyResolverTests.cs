using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.ProfitProtection;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.ProfitProtection;

public sealed class ProfitProtectionPolicyResolverTests
{
    private static readonly DateTime From = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static ProfitProtectionPolicyEntity Policy(long id, DateTime from, DateTime? to, string currency = "TRY")
    {
        var p = ProfitProtectionPolicyEntity.Create(
            currency, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0.5m,
            ProfitProtectionAdjustmentOrder.PlatformDiscountThenCommissionBenefit, from, to, $"PPOL-{id}");
        p.Id = id;
        return p;
    }

    private static readonly DateTime At = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Resolves_Single_Active_Policy()
    {
        var policies = new[] { Policy(1, From, null) };
        ProfitProtectionPolicyResolver.Resolve(policies, "TRY", At)!.Id.Should().Be(1);
    }

    [Fact]
    public void Ignores_Other_Currency()
    {
        var policies = new[] { Policy(1, From, null, "EUR") };
        ProfitProtectionPolicyResolver.Resolve(policies, "TRY", At).Should().BeNull();
    }

    [Fact]
    public void None_Active_Returns_Null()
    {
        var policies = new[] { Policy(1, new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), null) }; // future
        ProfitProtectionPolicyResolver.Resolve(policies, "TRY", At).Should().BeNull();
    }

    [Fact]
    public void Overlapping_Active_Policies_Throw_Conflict()
    {
        var policies = new[] { Policy(1, From, null), Policy(2, From, null) }; // both cover At
        Action act = () => ProfitProtectionPolicyResolver.Resolve(policies, "TRY", At);
        act.Should().Throw<AizenBusinessException>()
           .Where(e => e.ErrorCode == (int)PaymentErrorCode.ProfitProtectionPolicyConflict);
    }

    [Fact]
    public void FindOverlappingConflict_Detects_Same_Currency_Overlap_And_Excludes_Self()
    {
        var existing  = Policy(1, From, null);
        var candidate = Policy(0, From.AddYears(1), null);
        ProfitProtectionPolicyResolver.FindOverlappingConflict(candidate, new[] { existing }).Should().NotBeNull();

        var self = Policy(1, From, null);
        ProfitProtectionPolicyResolver.FindOverlappingConflict(self, new[] { existing }).Should().BeNull();
    }
}
