using Aizen.Core.Domain;
using Aizen.Core.UnitOfWork.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Core.Infrastructure.CQRS.Extention;

public static class UnitOfWorkFinder
{
    public static IAizenUnitOfWork FindUnitOfWorkForEntity<TEntity>(this IEnumerable<IAizenUnitOfWork> unitOfWorks)
        where TEntity : AizenEntity
    {
        foreach (var unitOfWork in unitOfWorks)
        {
            var dbContextType = unitOfWork.GetType().GetGenericArguments()[0];
            if (dbContextType.GetProperties()
                .Any(p => p.PropertyType.IsGenericType &&
                          p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                          p.PropertyType.GetGenericArguments()[0] == typeof(TEntity)))
            {
                return unitOfWork;
            }
        }

        return null;
    }
}