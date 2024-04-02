using Core.Membership.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Membership.ModelConfiguration
{
    public class AllowedIpConfiguration : IEntityTypeConfiguration<AllowedIp>
    {
        public void Configure(EntityTypeBuilder<AllowedIp> builder)
        {
            builder.HasMany(x => x.AllowedIpRoles);
            builder.HasMany(x => x.AllowedIpUsers);
            builder.ToTable("AllowedIp");
        }
    }
}