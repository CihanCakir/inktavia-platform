using Aizen.Core.Domain;

namespace Aizen.Core.CQRS.GenericHandler;

/// <summary>
/// Defense-in-depth guard for the generic Insert/Update/Delete command handlers.
///
/// The registration filter in Messagebus <c>BuilderExtensions</c> already prevents an
/// <c>AizenGenericConsumer&lt;T&gt;</c> from being wired for a <see cref="NoMessagebusSyncAttribute"/>
/// entity. This is the second, independent layer: even if such a generic command still
/// reaches a handler (mis-registration, a hand-crafted message, or a direct CQRS dispatch),
/// the handler refuses it — fail loud, before any repository is touched, so no row is
/// inserted/updated/deleted. A domain-authored, invariant-guarded entity can never be
/// persisted through the factory-bypassing generic path.
/// </summary>
internal static class GenericEntitySyncGuard
{
    public static void EnsureAllowed<TEntity>()
        where TEntity : AizenEntity
    {
        if (MessagebusSyncPolicy.IsGenericSyncBlocked(typeof(TEntity)))
        {
            throw new InvalidOperationException(
                $"Entity '{typeof(TEntity).FullName}' is marked [NoMessagebusSync] and cannot be " +
                "created, updated, or deleted through the generic messagebus/CQRS path. It is " +
                "domain-authored and must be built only via its validating factory; the generic " +
                "path would bypass its invariants. This operation was refused with no write.");
        }
    }
}
