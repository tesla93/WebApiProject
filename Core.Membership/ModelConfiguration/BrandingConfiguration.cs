using Core.Membership.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Membership.ModelConfiguration
{
    public class BrandingConfiguration : IEntityTypeConfiguration<Branding>
    {
        public void Configure(EntityTypeBuilder<Branding> builder)
        {
            builder.HasOne(p => p.LogoIcon)
                .WithOne()
                .HasForeignKey<Branding>(p => p.LogoIconId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(p => p.LogoImage)
                .WithOne()
                .HasForeignKey<Branding>(p => p.LogoImageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable("Brandings");
        }
    }
}
