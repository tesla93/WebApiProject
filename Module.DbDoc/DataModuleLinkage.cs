using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Module.Core.Web.Extensions;
using Module.DbDoc.Services;
using Module.ModuleLinkage;

namespace Mdoule.DbDoc
{
    public class DataModuleLinkage: IDataModuleLinkage
    {
        public async Task EnsureInitialData(IServiceScope serviceScope)
        {
            // set db descriptions
            var dbdocService = serviceScope.ServiceProvider.GetService<IDbDocToolService>();
            var hostingEnvironment = serviceScope.ServiceProvider.GetService<IWebHostEnvironment>();
            var dbdocSettings = serviceScope.ServiceProvider.GetService<Microsoft.Extensions.Options.IOptionsSnapshot<DBDocSettings>>();

            var fileData = dbdocService.GetDbStructureFromJson(hostingEnvironment.ContentRootPath + dbdocSettings.Value.FilePath);
            var dbData = dbdocService.GetDbStructure();
            if (fileData != null)
            {
                if ((!fileData.Timestamp.HasValue && (dbData == null || !dbData.Timestamp.HasValue)) ||
                     (fileData.Timestamp.HasValue && (dbData == null || !dbData.Timestamp.HasValue || fileData.Timestamp.Value > dbData.Timestamp.Value)))
                {
                    dbdocService.SaveDbStructureToDb(fileData);
                }
            }
            else if (dbData == null)
            {
                dbdocService.SaveDbStructureToDb(dbdocService.GetSyncedStructure());
            }
        }
    }
}