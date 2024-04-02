using Microsoft.EntityFrameworkCore;
using Core.Data;

namespace Project.SystemSettings
{
    public interface ISystemSettingsContext : IDbContext
    {
        DbSet<AppSettings> AppSettings { get; set; }
    }
}
