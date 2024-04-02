using System.Linq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Core.Crud;
using Core.Crud.Interfaces;
using Core.Filters;

namespace Core.Audit
{
    public interface IDataAuditService : IPagedCrudService<ChangeLogDTO>
    {
    }

    public class DataAuditService : PagedCrudService<ChangeLog, ChangeLogDTO>, IDataAuditService
    {
        public DataAuditService(IAuditDataContext context, IMapper mapper) : base(context, mapper)
        {
        }

        protected override IQueryable<ChangeLog> ConfigureDataReader(IQueryable<ChangeLog> entities) => entities.Include(l => l.ChangeLogItems);
    }
}
