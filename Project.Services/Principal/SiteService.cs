using AutoMapper;
using Core.Crud;
using Core.Data;
using Project.Data.DTO;
using Project.Data.Model;
using Project.Services.Interfaces;

namespace Project.Services.Principal
{
    public class SiteService : PagedCrudService<Site, SiteDTO>, ISiteService
    {
        public SiteService(IDbContext context, IMapper mapper) : base(context, mapper)
        {
        }

        // Moved to crudservice
        //public override async Task Delete(int id, CancellationToken cancellationToken = default)
        //{
        //    // 258-867. I do not want to go through all the related entities where Site can be referenced to, the list of these is huge.
        //    // Loading all the referenced data in a generic way is a bad approach as well.
        //    // Instead, I catch the DbUpdateException which is thrown when a record is referenced elsewhere in DB.
        //    try
        //    {
        //        await base.Delete(id, cancellationToken);
        //    }
        //    catch (DbUpdateException)
        //    {
        //        throw new BusinessException($"An error occurred while deleting site with Id: {id}. This site is used elsewhere in the system.");
        //    }
        //    catch (Exception)
        //    {
        //        throw new BusinessException($"Could not delete site with Id: {id}.");
        //    }
        //}
    }
}
