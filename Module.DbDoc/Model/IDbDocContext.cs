using Microsoft.EntityFrameworkCore;

namespace Module.DbDoc.Model
{
    public interface IDbDocContext
    {
        DbSet<ColumnMetadataQuery> ColumnMetadataQuery { get; set; }
        DbSet<TableMetadataQuery> TableMetadataQuery { get; set; }
    }
}