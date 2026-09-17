using Aizen.Core.Domain;
using Aizen.Core.InfoAccessor.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aizen.Core.UnitOfWork.Interceptors;

/// <summary>
/// Stamps audit fields (Create*/Modify*) and applies soft-delete on EVERY <c>SaveChanges</c> of any DbContext this
/// interceptor is registered on — not only the saves that flow through <see cref="AizenUnitOfWork{TContext}"/>.
///
/// Why this exists: audit stamping used to live ONLY in <c>AizenUnitOfWork.SaveChanges(Async)</c>. Repositories and
/// services that call <c>DbContext.SaveChangesAsync</c> directly (306 such call sites across 138 files at the time of
/// writing) bypassed it entirely, persisting rows with NULL <c>CreateDate/CreateUserId/CreateHost</c>. Any query that
/// filters/orders on <c>CreateDate</c> then silently drops those rows (<c>NULL &lt;= cutoff</c> is never true) — which
/// is exactly how the escrow auto-release safety-net job went blind to the stuck rows it exists to rescue. Moving the
/// logic to the DbContext layer makes it impossible to persist an <see cref="AizenEntityWithAudit"/> without stamps,
/// regardless of the save path (UnitOfWork, direct repo save, Hangfire job, MassTransit consumer, seeder).
///
/// Stamping now happens in EXACTLY ONE layer (this interceptor); <see cref="AizenUnitOfWork{TContext}"/> no longer
/// stamps, so there is no double-stamp with divergent timestamps.
///
/// DELETE SEMANTICS ARE DELIBERATELY NOT TOUCHED HERE. Historically only the UnitOfWork path converted deletes to
/// soft-deletes; the 306 direct-save call sites performed REAL deletes, and several depend on that: e.g.
/// ProviderServiceCategoryRepository.ReplaceForProfileAsync does delete-all + reinsert under a UNIQUE index on
/// (ProfileId, ServiceCategoryCode) — a soft-deleted survivor row would make the reinsert violate the index; the
/// Identity OTP/password-recovery services likewise physically clean up consumed request rows. Converting deletes
/// uniformly here would regress those flows (most read paths do not filter IsDeleted). The soft-delete conversion
/// therefore stays where it always was — <see cref="AizenUnitOfWork{TContext}"/> — which flips Deleted→Modified+
/// IsDeleted BEFORE the inner SaveChanges, so this interceptor then stamps the Modify* fields of that update.
/// </summary>
public sealed class AizenAuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAizenInfoAccessor _aizenInfoAccessor;

    public AizenAuditSaveChangesInterceptor(IAizenInfoAccessor aizenInfoAccessor)
        => _aizenInfoAccessor = aizenInfoAccessor;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
            return;

        // One timestamp for the whole save so every row in a single SaveChanges shares a consistent stamp.
        var now = DateTime.UtcNow;
        // Same resolution AizenUnitOfWork used: real user id when a user context exists, else fall back to 1 for
        // background paths (Hangfire jobs, MassTransit consumers, seeders) that have no authenticated user. ServerInfo
        // is populated from a process-wide singleton at container construction, so MachineName is always available.
        var userId = _aizenInfoAccessor.UserInfoAccessor.UserInfo != null
            ? _aizenInfoAccessor.UserInfoAccessor.UserInfo.UserId
            : 1;
        var host = _aizenInfoAccessor.ServerInfoAccessor.ServerInfo.MachineName;

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            // is-assignable-from (NOT the old IsSubclassOf): this also matches AizenEntityWithAudit itself. The base is
            // abstract so no direct instances exist today, but it is the correct, intent-matching check and future-proof.
            if (entry.Entity is not AizenEntityWithAudit entity)
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    entity.CreateDate   = now;
                    entity.CreateUserId = userId;
                    entity.CreateHost   = host;
                    break;

                case EntityState.Modified:
                    entity.ModifyDate   = now;
                    entity.ModifyUserId = userId;
                    entity.ModifyHost   = host;
                    break;

                // EntityState.Deleted is intentionally left alone — see the class doc: soft-delete conversion is a
                // UnitOfWork-path behavior; direct-save call sites rely on physical deletes (unique-index reinserts,
                // consumed-token cleanup).
            }
        }
    }
}
