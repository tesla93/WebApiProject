using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Z.EntityFramework.Plus;

namespace Core.Audit
{
    public class AuditDataContext : DbContext, IAuditDataContext
    {
        public AuditDataContext(DbContextOptions<AuditDataContext> options) : base(options)
        {
        }

        public DbSet<ChangeLog> ChangeLogs { get; set; }
        public DbSet<ChangeLogItem> ChangeLogItems { get; set; }

        public Dictionary<PropertyInfo, Type> FindKeys(Type type)
        {
            var entityType = Model.FindEntityType(type);
            if (entityType != null)
            {
                var foreignKeys = entityType.GetForeignKeys()
                    .SelectMany(k =>
                        k.Properties.Where(p => (p.ClrType == typeof(int) || p.ClrType == typeof(int?)) && p.PropertyInfo != null).Select(p => new
                        {
                            Property = p.PropertyInfo,
                            Principal = k.PrincipalEntityType.ClrType
                        })
                    ).ToArray();

                var primaryKeys = entityType.GetKeys()
                    .SelectMany(k =>
                        k.Properties.Where(p => (p.ClrType == typeof(int) || p.ClrType == typeof(int?)) && p.PropertyInfo != null).Select(p => new
                        {
                            Property = p.PropertyInfo,
                            Principal = type
                        })
                    ).ToArray();

                return foreignKeys.Concat(primaryKeys).GroupBy(p => p.Property).ToDictionary(g => g.Key, g => g.FirstOrDefault().Principal);
            }

            return null;
        }

        public BaseQueryFilter Filter<T>(Func<IQueryable<T>, IQueryable<T>> queryFilter, bool isEnabled = true)
        {
            return QueryFilterExtensions.Filter(this, queryFilter, isEnabled);
        }

        public BaseQueryFilter Filter<T>(object key, Func<IQueryable<T>, IQueryable<T>> queryFilter, bool isEnabled = true)
        {
            return QueryFilterExtensions.Filter(this, key, queryFilter, isEnabled);
        }
    }
}