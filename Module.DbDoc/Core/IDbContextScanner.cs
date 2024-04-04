using Module.Core.Data;
using Module.DbDoc.Model;
using System.Collections.Generic;

namespace Module.DbDoc.Core
{
    public interface IDbContextScanner
    {
        List<DbTable> Scan(IDbContext dbContext);
    }
}