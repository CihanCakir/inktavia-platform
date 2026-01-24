using Aizen.Core.IOC.Abstraction.Service;
using MiniUow;

namespace Aizen.Core.UnitOfWork.Abstraction;

public interface IAizenRepository<TEntity> : IAizenServiceScope, IRepository<TEntity> where TEntity : class
{

}