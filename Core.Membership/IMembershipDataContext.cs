using Core.Data;
using Core.Membership.Model;
using LockIP;
using Microsoft.EntityFrameworkCore;

namespace Core.Membership
{
    public interface IMembershipDataContext : IDbContext
    {
        DbSet<LoginAudit> Audits { get; set; }
        DbSet<Address> Addresses { get; set; }
        DbSet<Company> Companies { get; set; }
        DbSet<UserPasswordFailedHistory> UserPasswordFailedHistory { get; set; }
        DbSet<PasswordHistory> PasswordsHistory { get; set; }
        DbSet<Device> Devices { get; set; }
        DbSet<AuthenticationRequest> AuthenticationRequests { get; set; }
        DbSet<Branding> Brandings { get; set; }
        DbSet<AllowedIp> AllowedIp { get; set; }
        DbSet<LockedOutIp> LockedOutIp { get; set; }
        DbSet<AllowedIpUser> AllowedIpUser { get; set; }
        DbSet<AllowedIpRole> AllowedIpRole { get; set; }
        DbSet<UserGroup> UserGroups { get; set; }
        DbSet<ActivationToken> ActivationTokens { get; set; }
        DbSet<Permission> Permissions { get; set; }
        DbSet<RolePermission> RolePermissions { get; set; }
        DbSet<UserPermission> UserPermissions { get; set; }
    }
}