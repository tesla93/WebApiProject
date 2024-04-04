using Module.Core.Data;
using Module.DbDoc.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Module.DbDoc.Core
{
    public abstract class DatabaseScannerBase : IDatabaseScanner
    {
        private readonly IDbContext dbContext;
        private const int chunkSize = 20;

        public DatabaseScannerBase(IDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public List<ColumnMetadataQuery> ScanColumns(IList<IEntityType> entityTypes)
        {
            var result = new List<ColumnMetadataQuery>();

            if (!entityTypes.Any())
            {
                return result;
            }

            for (int i = 0; i <= entityTypes.Count / chunkSize; i++)
            {
                var queries = new List<string>();

                foreach (var entityType in entityTypes.Skip(i * chunkSize).Take(chunkSize))
                {
                    queries.Add(GenerateSqlQueryForColumnMetadata(entityType));
                }

                // result.AddRange(dbContext.Query<ColumnMetadataQuery>().FromSqlRaw(string.Join(" union all ", queries)).AsNoTracking().ToList());
            }

            return result;
        }

        public List<TableMetadataQuery> ScanTables(IList<IEntityType> entityTypes)
        {
            var result = new List<TableMetadataQuery>();

            if (!entityTypes.Any())
            {
                return result;
            }

            for (int i = 0; i <= entityTypes.Count / chunkSize; i++)
            {
                var rowCountQueries = new List<string>();

                foreach (var entityType in entityTypes.Skip(i * chunkSize).Take(chunkSize))
                {
                    rowCountQueries.Add(GenerateSqlQueryForRowCount(entityType));
                }

                result.AddRange(dbContext.Query<TableMetadataQuery>().FromSqlRaw(string.Join(" union all ", rowCountQueries)).AsNoTracking().ToList());
            }

            return result;
        }

        protected abstract string GetTableAlias(IEntityType entityType, string tableAlias = "t");

        protected abstract object GetColumnAlias(IProperty col, string tableAlias);
        protected abstract string GetLengthFunctionName();

        private string GenerateSqlQueryForRowCount(IEntityType entityType)
        {
            return $@"(select
                   '{entityType.GetTableName()}' as 'TableName', 
                   count(1) as 'NumRecords' 
                   from {GetTableAlias(entityType)})";
        }

        private string GenerateSqlQueryForColumnMetadata(IEntityType entityType)
        {
            var result = new List<string>();
            foreach (var propertyType in entityType.GetProperties())
            {
                result.Add(GenerateSqlQueryForColumnMetadata(propertyType, entityType));
            }
            return string.Join(" union all ", result);
        }

        private string GenerateSqlQueryForColumnMetadata(IProperty col, IEntityType entityType)
        {
            var tableAlias = "t";
            var allowedCast = IsTypeAllowedCastToString(col.ClrType) && !col.IsForeignKey() && !col.IsPrimaryKey();
            var function = col.ClrType == typeof(string) ? GetLengthFunctionName() : string.Empty;
            var min = allowedCast ? $"CONCAT(MIN({function}({GetColumnAlias(col, tableAlias)})), '')" : "''";
            var max = allowedCast ? $"CONCAT(MAX({function}({GetColumnAlias(col, tableAlias)})), '')" : "''";
            var distinct = $"count(distinct {GetColumnAlias(col, tableAlias)})";

            return $@"(select
                   '{entityType.GetTableName()}' as 'TableName', 
                   '{col.Name}' as 'Name', 
                   {min} as 'min', 
                   {max} as 'max', 
                   {distinct} as 'unique' 
                   from {GetTableAlias(entityType, tableAlias)})";
        }

        private bool IsTypeAllowedCastToString(Type type) =>
            !(type.Equals(typeof(Boolean)) || type.Equals(typeof(Nullable<Boolean>)) || type.Equals(typeof(Byte[])));
    }
}
