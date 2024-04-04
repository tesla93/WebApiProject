using Module.DbDoc.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Module.DbDoc.ModelConfiguration
{
    public class ColumnMetadataQueryConfiguration : IEntityTypeConfiguration<ColumnMetadataQuery>
    {
        // https://stackoverflow.com/questions/59427708/ef-core-3-dbquery-equivalent-functionality
        public void Configure(EntityTypeBuilder<ColumnMetadataQuery> builder) => builder.HasNoKey().ToView("ColumnMetadataQuery_view_name_that_doesnt_exist");
    }
}
