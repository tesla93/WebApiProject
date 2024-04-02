using Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Data.SqlServer
{
    // for migrations:
    // from web project:
    // create - dotnet ef migrations add <MigrationName> -p ../data.sqlserver -s ./ -c DataContext
    // update - dotnet ef database update -p ../data.sqlserver -s ./ -c DataContext

    // migration rollback
    // revert - dotnet ef database update <PreviousMigrationName> -p ../data.sqlserver -s ./ -c DataContext
    // remove - dotnet ef migrations remove -p ../data.sqlserver -s ./ -c DataContext

    public class DataContext : DataContextBase
    {
        public DataContext(DbContextOptions<DataContext> options, IDbServices dbServices) : base(options, dbServices)
        {
        }
    }
}
