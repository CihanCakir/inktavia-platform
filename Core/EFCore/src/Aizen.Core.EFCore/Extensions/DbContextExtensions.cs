using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Aizen.Core.Domain;
using Aizen.Core.EFCore.Converter;

namespace Aizen.Core.EFCore.Extensions;
public static class DbContextExtensions
{
    public static void ApplyIsNotDeletedFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType.GetCustomAttribute(typeof(IgnoreSoftDeleteFilterAttribute)) != null)
            {
                entityType.SetQueryFilter(null); // Soft-delete filtresini kaldır
            }
            else
            {
                var isDeletedProperty = entityType.FindProperty("IsDeleted");
                if (isDeletedProperty != null && isDeletedProperty.ClrType == typeof(bool))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "x");
                    var property = Expression.Property(parameter, isDeletedProperty.PropertyInfo);
                    var filter = Expression.Equal(property, Expression.Constant(false));
                    var lambda = Expression.Lambda(filter, parameter);

                    entityType.SetQueryFilter(lambda);
                }
            }
        }
    }

    public static void ApplyUtcDateTimeConverter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new UtcDateTimeConverter());
                }
            }
        }
    }
    public static void AddRowVersionToAllEntities(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AizenEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                            .Property<byte[]>("RowVersion")
                            .IsRowVersion();
            }
        }
    }

    public static void ConfigureDateTimeColumns(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp without time zone");
                }
            }
        }
    }
}