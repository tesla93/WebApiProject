using Core.Membership.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Membership.ModelConfiguration
{
    public class ActivationTokenConfiguration : IEntityTypeConfiguration<ActivationToken>
    {
        public void Configure(EntityTypeBuilder<ActivationToken> builder)
        {
            builder.Property(x => x.Token).HasMaxLength(450);
            builder.ToTable("ActivationTokens");
        }
    }
}