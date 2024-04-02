using Core.Membership.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Membership.ModelConfiguration
{
    public class LoginAuditConfiguration : IEntityTypeConfiguration<LoginAudit>
    {
        public void Configure(EntityTypeBuilder<LoginAudit> builder) => builder.ToTable("Audits");
    }
}