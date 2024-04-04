using Microsoft.EntityFrameworkCore;
using Module.DbDoc.Model;

namespace Module.DbDoc
{
    public static class ServiceCollectionExtensions
    {
        public static void OnDbDocModelCreating(this ModelBuilder builder)
        {
            // https://stackoverflow.com/questions/59427708/ef-core-3-dbquery-equivalent-functionality
            builder.Entity<ColumnMetadataQuery>()
                .HasNoKey()
                .ToView("ColumnMetadataQuery_view_name_that_doesnt_exist");

            builder.Entity<TableMetadataQuery>()
                .HasNoKey()
                .ToView("TableMetadataQuery_view_name_that_doesnt_exist");
        }
    }
}