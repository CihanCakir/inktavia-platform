namespace Aizen.Core.Domain;

/// <summary>
/// Single source of truth for "is this entity blocked from the generic messagebus
/// sync path?". Both the registration filter (Messagebus <c>BuilderExtensions</c>) and
/// the defense-in-depth handler guards (CQRS generic Insert/Update/Delete handlers)
/// call this, so the two layers can never disagree.
///
/// An entity is blocked when it carries <see cref="NoMessagebusSyncAttribute"/>
/// (inherited), i.e. it is a domain-authored, invariant-guarded type that must only
/// be built via its validating factory.
/// </summary>
public static class MessagebusSyncPolicy
{
    /// <summary>
    /// True when <paramref name="entityType"/> must never flow through the generic
    /// consumer / generic Insert-Update-Delete commands.
    /// </summary>
    public static bool IsGenericSyncBlocked(Type entityType)
        => entityType is not null &&
           Attribute.IsDefined(entityType, typeof(NoMessagebusSyncAttribute), inherit: true);
}
