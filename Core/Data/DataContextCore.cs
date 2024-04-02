using Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Core.Data
{
    public class DataContextCore<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> : IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>, IDbContext
       where TUser : IdentityUser<TKey>
       where TRole : IdentityRole<TKey>
       where TKey : IEquatable<TKey>
       where TUserClaim : IdentityUserClaim<TKey>
       where TUserRole : IdentityUserRole<TKey>
       where TUserLogin : IdentityUserLogin<TKey>
       where TRoleClaim : IdentityRoleClaim<TKey>
       where TUserToken : IdentityUserToken<TKey>
    {
        protected readonly IMultiTenancyService _multiTenancyService;
        protected readonly int? _tenantId;
        protected DataContextCore(DbContextOptions options, IDbServices dbServices) : base(options)
        {
            _multiTenancyService = dbServices.GetMultiTenancyService();
            if (_multiTenancyService != null)
            {
                _tenantId = _multiTenancyService.GetTenancyId();
                MultiTenancyFilter(_tenantId);
            }
        }

        private void MultiTenancyFilter(int? tenantId)
        {
            var method = typeof(DataContextCore<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                                        .Single(t => t.IsGenericMethod && t.Name == "FilterTenancyEntity");

            var entityes = Model.GetEntityTypes();
            foreach (var entity in entityes)
            {
                if (typeof(ITenantEntity).IsAssignableFrom(entity.ClrType))
                {
                    var genericMethod = method.MakeGenericMethod(entity.ClrType);
                    genericMethod.Invoke(this, null);
                }
            }
        }

        public void FilterTenancyEntity<T>() where T : ITenantEntity
        {
            Filter<T>(x => x.Where(y => y.TenantId == _tenantId));
        }

        public BaseQueryFilter Filter<T>(Func<IQueryable<T>, IQueryable<T>> queryFilter, bool isEnabled = true)
        {
            return QueryFilterExtensions.Filter(this, queryFilter, isEnabled);
        }

        public BaseQueryFilter Filter<T>(object key, Func<IQueryable<T>, IQueryable<T>> queryFilter, bool isEnabled = true)
        {
            return QueryFilterExtensions.Filter(this, key, queryFilter, isEnabled);
        }

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


        public override int SaveChanges()
        {
            CheckTenantId();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            CheckTenantId();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            CheckTenantId();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            CheckTenantId();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void CheckTenantId()
        {
            var addEntities = ChangeTracker.Entries<ITenantEntity>()
                .Where(p => p.State == EntityState.Added).ToList();
            var now = DateTime.UtcNow;

            foreach (var newEntity in addEntities)
            {
                newEntity.Entity.TenantId = _tenantId;
            }
        }
    }
}