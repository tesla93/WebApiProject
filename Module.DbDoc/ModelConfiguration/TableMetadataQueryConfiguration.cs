using Module.DbDoc.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Module.DbDoc.ModelConfiguration
{
    public class TableMetadataQueryConfiguration : IEntityTypeConfiguration<TableMetadataQuery>
    {
        // https://stackoverflow.com/questions/59427708/ef-core-3-dbquery-equivalent-functionality
        public void Configure(EntityTypeBuilder<TableMetadataQuery> builder) => builder.HasNoKey().ToView("TableMetadataQuery_view_name_that_doesnt_exist");
    }
}
