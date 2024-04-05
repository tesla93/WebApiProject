using Core.Audit;
using Core.Membership.Model;
using Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.Data.Models;

namespace Project.Data
{
    public abstract class DataContextBase : AuditableDataContextCore<User, Role, string,
        IdentityUserClaim<string>, UserRole, IdentityUserLogin<string>, IdentityRoleClaim<string>,
        IdentityUserToken<string>>
    {
        public DataContextBase(DbContextOptions options, IDbServices dbServices) : base(options, dbServices)
        {
            
        }
        public DbSet<Order> Orders { get; set; }
    }
}
