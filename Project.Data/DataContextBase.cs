using Core.Audit;
using Core.Membership;
using Core.Membership.Model;
using Core.Services;
using Data.Model;
using FileStorage;
using LockIP;
using Messages.Templates;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.SystemSettings;

namespace Data
{
    public abstract class DataContextBase : AuditableDataContextCore<User, Role, string,
        IdentityUserClaim<string>, UserRole, IdentityUserLogin<string>, IdentityRoleClaim<string>,
        IdentityUserToken<string>>, IDataContext
    {
        protected DataContextBase(DbContextOptions options, IDbServices dbServices) : base(options, dbServices)
        {
        }

       
        #region Membership
        public DbSet<ActivationToken> ActivationTokens { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<AllowedIp> AllowedIp { get; set; }
        public DbSet<AllowedIpUser> AllowedIpUser { get; set; }
        public DbSet<AllowedIpRole> AllowedIpRole { get; set; }
        public DbSet<LoginAudit> Audits { get; set; }
        public DbSet<AuthenticationRequest> AuthenticationRequests { get; set; }
        public DbSet<Branding> Brandings { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<PasswordHistory> PasswordsHistory { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<UserPasswordFailedHistory> UserPasswordFailedHistory { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        #endregion

     
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<EmailTemplateParameter> EmailTemplateParameters { get; set; }
        public DbSet<LockedOutIp> LockedOutIp { get; set; }
        public DbSet<FileDetails> FilesDetails { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.OnMembershipModelCreating();
            //builder.OnDbDocModelCreating();

            //builder.Entity<RreslistUserRole>()
            //.HasKey(c => new { c.RoleId, c.UserId });


            #region Setup
            //builder.Entity<Crband>()
            //    .HasIndex(u => new { u.Proposed, u.SiteId })
            //    .IsUnique();

            //builder.Entity<Crstatus>()
            //    .HasIndex(u => new { u.Status, u.SiteId })
            //    .IsUnique();

            //builder.Entity<Cstatus>()
            //    .HasIndex(u => new { u.Status, u.SiteId })
            //    .IsUnique();
            #endregion

           

                
                

            //builder.Entity<Enquiry>(entity =>
            //{
            //    entity.HasOne(d => d.YardEstimate)
            //        .WithMany()
            //        .HasForeignKey(x => x.YardEstimateId)
            //        .OnDelete(DeleteBehavior.Cascade)
            //        .HasConstraintName("FK_maf018_maf019_YardEstimateId");

            //    entity.HasOne(d => d.YardWork)
            //        .WithMany()
            //        .HasForeignKey(x => x.YardWorkId)
            //        .OnDelete(DeleteBehavior.Cascade)
            //        .HasConstraintName("FK_maf018_maf019_YardWorkId");
            //});
        }
    }
}