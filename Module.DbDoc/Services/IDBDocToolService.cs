using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Module.DbDoc.Core;
using Module.DbDoc.Model;

namespace Module.DbDoc.Services
{
    public interface IDbDocToolService
    {
        DbStructure GetDbStructureFromJson(string path);
        void SaveDbStructureToDb(DbStructure obj);
        DbStructure GetSyncedStructure(DbStructure obj = null);
        void SyncAndSaveStructure(DbStructure obj);
        Task<bool> SendToGit(string email, CancellationToken cancellationToken);
        IDbTableDumpProvider GetDbTableDumpProvider(string clrType);
        void SaveItemTypes(List<DbColumnType> items);
        DbStructure GetDbStructure();
        List<DbColumnType> GetColumnTypes();
    }
}