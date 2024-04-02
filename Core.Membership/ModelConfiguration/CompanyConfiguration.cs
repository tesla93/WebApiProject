using Core.Membership.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Membership.ModelConfiguration
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.HasOne(x => x.Address)
                .WithOne(x => x.Company)
                .HasForeignKey<Company>(x => x.AddressId)
                .OnDelete(DeleteBehavior.SetNull);
            builder.HasOne(x => x.Branding)
                .WithOne(x => x.Company)
                .HasForeignKey<Company>(x => x.BrandingId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.ToTable("Companies");
        }
    }
}
