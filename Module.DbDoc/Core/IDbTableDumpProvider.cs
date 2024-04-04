using Module.Core.Filters;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;

namespace Module.DbDoc.Core
{
    public interface IDbTableDumpProvider
    {
        List<Tuple<string, string>> GetTableColumnsList(string tableName);
        PageResult<dynamic> GetTableDump(string tablename, FilterCommand command);
        PageResult<dynamic> GetTableDumpPage<T>(IEntityType entity, FilterCommand command) where T : class;
    }
}