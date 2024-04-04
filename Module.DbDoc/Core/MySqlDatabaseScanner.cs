using Module.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Module.DbDoc.Core
{
    internal class MySqlDatabaseScanner : DatabaseScannerBase
    {
        public MySqlDatabaseScanner(IDbContext dbContext) : base(dbContext)
        {
        }

        protected override string GetTableAlias(IEntityType entityType, string tableAlias = "t") => $"`{entityType.GetTableName()}` as {tableAlias}";

        protected override string GetLengthFunctionName() => "LENGTH";

        protected override object GetColumnAlias(IProperty property, string tableAlias) => $"{tableAlias}.{property.Name}";
    }
}