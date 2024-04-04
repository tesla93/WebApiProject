using Module.Core.Data;
using Module.DbDoc.Model;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;

namespace Module.DbDoc.Core
{
    public interface IDatabaseScanner
    {
        List<ColumnMetadataQuery> ScanColumns(IList<IEntityType> entityTypes);
        List<TableMetadataQuery> ScanTables(IList<IEntityType> entityTypes);
    }
}