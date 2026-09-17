using Aizen.Core.Domain;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.UnitOfWork.Abstraction;
using Microsoft.EntityFrameworkCore;
using MiniUow;
using System.Collections.Concurrent;

namespace Aizen.Core.UnitOfWork;

public class AizenUnitOfWork<TContext> : IAizenUnitOfWork<TContext>, IAizenUnitOfWork
    where TContext : DbContext
{
    public DbContext DbContext => _unitOfWork.Context;

    public TContext Context => _unitOfWork.Context;

    private readonly IUnitOfWork<TContext> _unitOfWork;
    private readonly ConcurrentDictionary<Type, object> _repositories;
    private readonly IAizenInfoAccessor _aizenInfoAccessor;

    public AizenUnitOfWork(IUnitOfWork<TContext> unitOfWork, IAizenInfoAccessor aizenInfoAccessor)
    {
        _unitOfWork = unitOfWork;
        _repositories = new ConcurrentDictionary<Type, object>();
        _aizenInfoAccessor = aizenInfoAccessor;
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
    }

    public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
    {
        var respository = _unitOfWork.GetRepository<TEntity>();
        return new AizenRepository<TEntity>(respository);
    }

    // Audit stamping (Create*/Modify*) moved to AizenAuditSaveChangesInterceptor at the DbContext layer, so it runs
    // on EVERY save path — not just through this UnitOfWork. Stamping happens in exactly ONE place (the interceptor),
    // preventing double-stamps with divergent timestamps. _aizenInfoAccessor is retained on the ctor for DI/behavioral
    // compatibility (the interceptor is the sole consumer of user/host now).
    //
    // SOFT-DELETE conversion, however, stays HERE — it always was a UnitOfWork-path behavior. Direct-save call sites
    // rely on physical deletes (delete-all+reinsert under unique indexes, consumed-token cleanup), so the interceptor
    // must not convert them. We only flip Deleted → Modified + IsDeleted; the interceptor then sees the Modified entry
    // during the inner SaveChanges and applies the Modify* stamps — still a single stamping layer.
    public int SaveChanges()
    {
        ConvertDeletesToSoftDeletes();
        return _unitOfWork.SaveChanges();
    }

    public Task<int> SaveChangesAsync()
    {
        ConvertDeletesToSoftDeletes();
        return _unitOfWork.SaveChangesAsync();
    }

    private void ConvertDeletesToSoftDeletes()
    {
        var deletedEntities = Context.ChangeTracker
                             .Entries()
                             .Where(x => x.State == EntityState.Deleted && x.Entity is AizenEntityWithAudit)
                             .ToList();

        foreach (var entry in deletedEntities)
        {
            var entity = (AizenEntityWithAudit)entry.Entity;
            entity.IsDeleted = true;
            entry.State      = EntityState.Modified; // Fiziksel silme yok — soft-delete: interceptor Modify* damgalarını basar.
        }
    }
}