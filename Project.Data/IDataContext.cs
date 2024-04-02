using Core.Data;
using Core.Membership;
using FileStorage;
using Messages.Templates;
using Microsoft.EntityFrameworkCore;
using Module.SystemSettings;

namespace Project.Data
{
    public interface IDataContext : IDbContext,
        IFileDetailsContext,
        IMessageTemplatesContext,
        //ILoadingTimeContext,
        //IMetadataContext<Metadata, User>,
        //IDbDocContext,
        IMembershipDataContext
        //IStaticPageContext,
        //IHangfireSettingsContext
    {
        DbSet<AppSettings> AppSettings { get; set; }
    }
}
