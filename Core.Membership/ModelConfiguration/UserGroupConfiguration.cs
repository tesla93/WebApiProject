using Core.Membership.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Membership.ModelConfiguration
{
    public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
    {
        public void Configure(EntityTypeBuilder<UserGroup> builder)
        {
            builder.HasKey(x => new {x.UserId, x.GroupId});
            builder.HasOne(x => x.User)
                .WithMany(x => x.UserGroups)
                .HasForeignKey(x => x.UserId);
            builder.HasOne(x => x.Group)
                .WithMany(x => x.UserGroups)
                .HasForeignKey(x => x.GroupId);

            builder.ToTable("AspNetUserGroups");
        }
    }
}
