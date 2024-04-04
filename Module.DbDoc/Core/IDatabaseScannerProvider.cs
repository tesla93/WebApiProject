using Module.Core.Data;

namespace Module.DbDoc.Core
{
    public interface IDatabaseScannerProvider
    {
        IDatabaseScanner GetScanner(IDbContext dbContext);
    }
}