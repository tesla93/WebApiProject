using Microsoft.EntityFrameworkCore;
using Core.Data;

namespace Module.SystemSettings
{
    public interface ISystemSettingsContext : IDbContext
    {
        DbSet<AppSettings> AppSettings { get; set; }
    }
}
