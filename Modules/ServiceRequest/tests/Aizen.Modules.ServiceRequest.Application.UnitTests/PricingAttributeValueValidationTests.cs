using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;
using Aizen.Modules.ServiceRequest.Application.Services.Pricing;
using Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// S2b — the pure pricing-attribute value validation core. Descriptive metadata; the tests assert the fail-loud contract:
/// valid/invalid lookup, required-missing, wrong-type, number range, duplicates, unknown definition.
/// </summary>
public sealed class PricingAttributeValueValidationTests
{
    // ── Builders ────────────────────────────────────────────────────────────────
    private static PricingAttributeDefinitionEntity LookupDef(string code = "PAINT_TYPE", string group = "PAINT_TYPE", bool required = false)
        => PricingAttributeDefinitionEntity.Create(code, "Boya Tipi", "Paint Type", PricingAttributeDataType.Lookup,
            group, required, 10, null, null, new[] { "PAINTING" });

    private static PricingAttributeDefinitionEntity NumberDef(string code = "COATS", decimal? min = 1, decimal? max = 5, bool required = false)
        => PricingAttributeDefinitionEntity.Create(code, "Kat", "Coats", PricingAttributeDataType.Number,
            null, required, 20, min, max, new[] { "PAINTING" });

    private static Dictionary<string, IReadOnlySet<string>> PaintMembers()
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["PAINT_TYPE"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ANTIFOULING", "EPOXY", "TOPCOAT", "PRIMER", "GELCOAT" },
        };

    private static OfferLineAttributeValueRequest Lookup(string code, string item)
        => new() { DefinitionCode = code, ValueLookupItemCode = item };

    private static void Act(IReadOnlyList<PricingAttributeDefinitionEntity> defs,
        IReadOnlyDictionary<string, IReadOnlySet<string>> members, params OfferLineAttributeValueRequest[] values)
        => PricingAttributeValueValidation.Validate(defs, members, values);

    // ── Valid lookup ────────────────────────────────────────────────────────────
    [Fact]
    public void Valid_LookupMember_Passes()
    {
        var act = () => Act(new[] { LookupDef() }, PaintMembers(), Lookup("PAINT_TYPE", "ANTIFOULING"));
        act.Should().NotThrow();
    }

    [Fact]
    public void Valid_LookupMember_IsCaseInsensitive()
    {
        var act = () => Act(new[] { LookupDef() }, PaintMembers(), Lookup("paint_type", "antifouling"));
        act.Should().NotThrow();
    }

    // ── Invalid lookup (not a member) ────────────────────────────────────────────
    [Fact]
    public void Invalid_LookupNonMember_Throws()
    {
        var act = () => Act(new[] { LookupDef() }, PaintMembers(), Lookup("PAINT_TYPE", "NEON_GLITTER"));
        act.Should().Throw<AizenBusinessException>().WithMessage("*INVALID_LOOKUP_ITEM*");
    }

    // ── Required missing ─────────────────────────────────────────────────────────
    [Fact]
    public void Required_Missing_Throws()
    {
        // A required PAINT_TYPE with no value in the set.
        var act = () => Act(new[] { LookupDef(required: true) }, PaintMembers());
        act.Should().Throw<AizenBusinessException>().WithMessage("*REQUIRED_MISSING*");
    }

    [Fact]
    public void Required_Present_Passes()
    {
        var act = () => Act(new[] { LookupDef(required: true) }, PaintMembers(), Lookup("PAINT_TYPE", "EPOXY"));
        act.Should().NotThrow();
    }

    // ── Wrong type ───────────────────────────────────────────────────────────────
    [Fact]
    public void WrongType_NumberValueForLookupDef_Throws()
    {
        var v = new OfferLineAttributeValueRequest { DefinitionCode = "PAINT_TYPE", ValueNumber = 3 };
        var act = () => Act(new[] { LookupDef() }, PaintMembers(), v);
        act.Should().Throw<AizenBusinessException>().WithMessage("*WRONG_TYPE*");
    }

    [Fact]
    public void WrongType_LookupValueForNumberDef_Throws()
    {
        var v = new OfferLineAttributeValueRequest { DefinitionCode = "COATS", ValueLookupItemCode = "ANTIFOULING" };
        var act = () => Act(new[] { NumberDef() }, PaintMembers(), v);
        act.Should().Throw<AizenBusinessException>().WithMessage("*WRONG_TYPE*");
    }

    // ── Number range ─────────────────────────────────────────────────────────────
    [Fact]
    public void Number_OutOfRange_Throws()
    {
        var v = new OfferLineAttributeValueRequest { DefinitionCode = "COATS", ValueNumber = 9 };
        var act = () => Act(new[] { NumberDef(min: 1, max: 5) }, PaintMembers(), v);
        act.Should().Throw<AizenBusinessException>().WithMessage("*NUMBER_OUT_OF_RANGE*");
    }

    [Fact]
    public void Number_InRange_Passes()
    {
        var v = new OfferLineAttributeValueRequest { DefinitionCode = "COATS", ValueNumber = 3 };
        var act = () => Act(new[] { NumberDef(min: 1, max: 5) }, PaintMembers(), v);
        act.Should().NotThrow();
    }

    // ── Unknown / duplicate ──────────────────────────────────────────────────────
    [Fact]
    public void UnknownDefinition_Throws()
    {
        var act = () => Act(new[] { LookupDef() }, PaintMembers(), Lookup("NOT_A_DEF", "ANTIFOULING"));
        act.Should().Throw<AizenBusinessException>().WithMessage("*UNKNOWN_DEFINITION*");
    }

    [Fact]
    public void DuplicateValue_Throws()
    {
        var act = () => Act(new[] { LookupDef() }, PaintMembers(),
            Lookup("PAINT_TYPE", "ANTIFOULING"), Lookup("PAINT_TYPE", "EPOXY"));
        act.Should().Throw<AizenBusinessException>().WithMessage("*DUPLICATE_VALUE*");
    }
}
