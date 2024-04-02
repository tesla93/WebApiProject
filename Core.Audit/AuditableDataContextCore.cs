using Core.Data;
using Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Audit
{
    public abstract class AuditableDataContextCore<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>
        : DataContextCore<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>, IDbContext
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserLogin : IdentityUserLogin<TKey>
        where TRoleClaim : IdentityRoleClaim<TKey>
        where TUserToken : IdentityUserToken<TKey>
    {
        protected readonly IAuditWrapper _auditWrapper;

        protected AuditableDataContextCore(DbContextOptions options, IDbServices dbServices) : base(options, dbServices) =>
            _auditWrapper = dbServices.GetAuditWrapper();

        public override int SaveChanges()
        {
            if (_auditWrapper == null)
            {
                return base.SaveChanges();
            }

            OnBeforeSaveChanges();
            var result = base.SaveChanges();
            OnAfterSaveChanges().Wait();
            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_auditWrapper == null)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }

            OnBeforeSaveChanges();
            try
            {
                var result = await base.SaveChangesAsync(cancellationToken);
                await OnAfterSaveChanges();
                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void OnBeforeSaveChanges() =>
            _auditWrapper.OnBeforeSaveChanges(ChangeTracker.Entries().ToArray());

        private Task OnAfterSaveChanges() =>
            _auditWrapper.OnAfterSaveChanges();
    }
}