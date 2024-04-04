using Module.Core.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace Module.DbDoc.Core
{
    public class DatabaseScannerProvider : IDatabaseScannerProvider
    {
        public IDatabaseScanner GetScanner(IDbContext dbContext)
        {
            if (dbContext.Database.IsMySql())
                return new MySqlDatabaseScanner(dbContext);
            if (dbContext.Database.IsSqlServer())
                return new SqlServerDatabaseScanner(dbContext);

            throw new ApplicationException("DbDoc: Unsupported database type");
        }
    }
}
