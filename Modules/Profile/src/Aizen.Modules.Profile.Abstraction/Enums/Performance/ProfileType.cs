namespace Aizen.Modules.Profile.Abstraction.Enums.Performance;

/// <summary>
/// Identifies which actor type a performance snapshot belongs to.
/// The engine is generic: performance is calculated for Providers, Participants,
/// and Owners using the same entity schema, distinguished by this field.
/// </summary>
public enum ProfileType
{
    /// <summary>Marine service provider.</summary>
    Provider    = 1,

    /// <summary>Yacht/boat owner (boat owner, service requester).</summary>
    Owner       = 2,

    /// <summary>Marketplace participant / buyer.</summary>
    Participant = 3,
}
