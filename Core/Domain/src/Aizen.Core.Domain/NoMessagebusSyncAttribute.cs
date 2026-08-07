namespace Aizen.Core.Domain;

/// <summary>
/// Marks a domain-authored, invariant-guarded entity as EXEMPT from the generic
/// messagebus sync path (<c>AizenGenericConsumer&lt;T&gt;</c> →
/// <c>AizenInsert/Update/DeleteEntityCommand&lt;T&gt;</c>).
///
/// The generic path deserializes an inbound message straight onto the entity and
/// persists it, bypassing the validating factory and every invariant. For immutable
/// financial snapshots / ledger / allocation records (built only via a validating
/// factory, no public mutators) that would allow an insert/update/<b>delete</b> with
/// none of the guards. Marking such an entity opts it out.
///
/// Two layers enforce the opt-out (see <see cref="MessagebusSyncPolicy"/>):
///   1. Registration filter — <c>BuilderExtensions.AddAizenMessagebus</c> skips
///      <c>AizenGenericConsumer&lt;T&gt;</c> for a marked type, so no generic consumer
///      is ever wired for it.
///   2. Defense-in-depth guard — the generic Insert/Update/Delete command handlers
///      refuse a marked type at runtime (fail loud, no write), so the invariant can
///      never be bypassed even via a mis-registration or a hand-crafted message.
///
/// <see cref="Inherited"/> is <c>true</c>: subclasses of a marked type inherit the opt-out.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class NoMessagebusSyncAttribute : Attribute
{
}
