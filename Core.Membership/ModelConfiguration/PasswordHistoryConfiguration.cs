using Core.Membership.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Membership.ModelConfiguration
{
    public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
    {
        public void Configure(EntityTypeBuilder<PasswordHistory> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Password);
            builder.Property(p => p.CreateDate);
            builder.Property(p => p.UserId);
            builder.ToTable("PasswordHistory");
        }
    }
}
