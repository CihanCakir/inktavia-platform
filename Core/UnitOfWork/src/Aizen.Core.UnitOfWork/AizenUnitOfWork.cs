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

    public int SaveChanges()
    {
        var addedEntities = Context.ChangeTracker
                                 .Entries()
                                 .Where(x => x.State == EntityState.Added && x.Entity.GetType().IsSubclassOf(typeof(AizenEntityWithAudit)))
                                 .ToList();

        foreach (var entry in addedEntities)
        {
            var entity = entry.Entity as AizenEntityWithAudit;
            if (entity != null)
            {
                entity.CreateDate = DateTime.Now; // Şu anki zamanı atayabilirsiniz.
                entity.CreateUserId = _aizenInfoAccessor.UserInfoAccessor.UserInfo != null ? _aizenInfoAccessor.UserInfoAccessor.UserInfo.UserId : 1;// Burada oturum bilgilerinden ya da diğer kaynaklardan kullanıcı adını alıp atayabilirsiniz.
                entity.CreateHost = _aizenInfoAccessor.ServerInfoAccessor.ServerInfo.MachineName;
            }
        }

        var updatedEntities = Context.ChangeTracker
                             .Entries()
                             .Where(x => x.State == EntityState.Modified && x.Entity.GetType().IsSubclassOf(typeof(AizenEntityWithAudit)))
                             .ToList();

        foreach (var entry in updatedEntities)
        {
            var entity = entry.Entity as AizenEntityWithAudit;
            if (entity != null)
            {
                entity.ModifyDate = DateTime.Now; // Güncelleme zamanını atayabilirsiniz.
                entity.ModifyUserId = _aizenInfoAccessor.UserInfoAccessor.UserInfo != null ? _aizenInfoAccessor.UserInfoAccessor.UserInfo.UserId : 1; // Burada oturum bilgilerinden ya da diğer kaynaklardan kullanıcı adını alıp atayabilirsiniz.
                entity.ModifyHost = _aizenInfoAccessor.ServerInfoAccessor.ServerInfo.MachineName;
            }
        }

        var deletedEntities = Context.ChangeTracker
                             .Entries()
                             .Where(x => x.State == EntityState.Deleted && x.Entity.GetType().IsSubclassOf(typeof(AizenEntityWithAudit)))
                             .ToList();

        foreach (var entry in deletedEntities)
        {
            var entity = entry.Entity as AizenEntityWithAudit;
            if (entity != null)
            {
                entity.IsDeleted = true;
                entry.State = EntityState.Modified; // Nesnenin durumunu "Modified" (Güncellendi) olarak değiştiriyoruz çünkü artık fiziksel olarak silmiyoruz.
                entity.ModifyDate = DateTime.Now; // Güncelleme zamanını atayabilirsiniz.
                entity.ModifyUserId =  _aizenInfoAccessor.UserInfoAccessor.UserInfo != null ? _aizenInfoAccessor.UserInfoAccessor.UserInfo.UserId : 1; // Burada oturum bilgilerinden ya da diğer kaynaklardan kullanıcı adını alıp atayabilirsiniz.
                entity.ModifyHost = _aizenInfoAccessor.ServerInfoAccessor.ServerInfo.MachineName;
            }
        }

        return _unitOfWork.SaveChanges();
    }

    public Task<int> SaveChangesAsync()
    {

        var addedEntities = Context.ChangeTracker
                                 .Entries()
                                 .Where(x => x.State == EntityState.Added && x.Entity.GetType().IsSubclassOf(typeof(AizenEntityWithAudit)))
                                 .ToList();

        foreach (var entry in addedEntities)
        {
            var entity = entry.Entity as AizenEntityWithAudit;
            if (entity != null)
            {
                entity.CreateDate = DateTime.Now; // Şu anki zamanı atayabilirsiniz.
                entity.CreateUserId = _aizenInfoAccessor.UserInfoAccessor.UserInfo == null ? 1 : _aizenInfoAccessor.UserInfoAccessor.UserInfo.UserId; // Burada oturum bilgilerinden ya da diğer kaynaklardan kullanıcı adını alıp atayabilirsiniz.
                entity.CreateHost = _aizenInfoAccessor.ServerInfoAccessor.ServerInfo.MachineName;
            }
        }

        var updatedEntities = Context.ChangeTracker
                             .Entries()
                             .Where(x => x.State == EntityState.Modified && x.Entity.GetType().IsSubclassOf(typeof(AizenEntityWithAudit)))
                             .ToList();

        foreach (var entry in updatedEntities)
        {
            var entity = entry.Entity as AizenEntityWithAudit;
            if (entity != null)
            {
                entity.ModifyDate = DateTime.Now; // Güncelleme zamanını atayabilirsiniz.
                entity.ModifyUserId = _aizenInfoAccessor.UserInfoAccessor.UserInfo == null ? 1 : _aizenInfoAccessor.UserInfoAccessor.UserInfo.UserId; // Burada oturum bilgilerinden ya da diğer kaynaklardan kullanıcı adını alıp atayabilirsiniz.
                entity.ModifyHost = _aizenInfoAccessor.ServerInfoAccessor.ServerInfo.MachineName;
            }
        }

        var deletedEntities = Context.ChangeTracker
                             .Entries()
                             .Where(x => x.State == EntityState.Deleted && x.Entity.GetType().IsSubclassOf(typeof(AizenEntityWithAudit)))
                             .ToList();

        foreach (var entry in deletedEntities)
        {
            var entity = entry.Entity as AizenEntityWithAudit;
            if (entity != null)
            {
                entity.IsDeleted = true;
                entry.State = EntityState.Modified; // Nesnenin durumunu "Modified" (Güncellendi) olarak değiştiriyoruz çünkü artık fiziksel olarak silmiyoruz.
                entity.ModifyDate = DateTime.Now; // Güncelleme zamanını atayabilirsiniz.
                entity.ModifyUserId = _aizenInfoAccessor.UserInfoAccessor.UserInfo == null ? 1 : _aizenInfoAccessor.UserInfoAccessor.UserInfo.UserId; // Burada oturum bilgilerinden ya da diğer kaynaklardan kullanıcı adını alıp atayabilirsiniz.
                entity.ModifyHost = _aizenInfoAccessor.ServerInfoAccessor.ServerInfo.MachineName;
            }
        }
        return _unitOfWork.SaveChangesAsync();
    }
}