using Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Core.Audit
{
    public interface IAuditDataContext : IDbContext
    {
        DbSet<ChangeLog> ChangeLogs { get; set; }

        DbSet<ChangeLogItem> ChangeLogItems { get; set; }
    }
}
