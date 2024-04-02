using Core.Membership.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Membership.ModelConfiguration
{
    public class AllowedIpUserConfiguration : IEntityTypeConfiguration<AllowedIpUser>
    {
        public void Configure(EntityTypeBuilder<AllowedIpUser> builder)
        {
            builder.HasOne(x => x.AllowedIp)
                .WithMany(x => x.AllowedIpUsers)
                .HasForeignKey(x => x.AllowedIpId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany(x => x.AllowedIpUser)
                .HasForeignKey(x => x.UserId);
                //.OnDelete(DeleteBehavior.Cascade); - recover then

            builder.ToTable("AllowedIpUsers");
        }
    }
}