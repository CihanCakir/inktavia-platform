using Microsoft.EntityFrameworkCore;
using MiniUow;

namespace Aizen.Core.UnitOfWork.Abstraction;

public interface IAizenUnitOfWork<TContext> : IAizenUnitOfWork, IUnitOfWork<TContext>
    where TContext : DbContext
{
}

public interface IAizenUnitOfWork : IUnitOfWork
{
    public DbContext DbContext { get; }
}