using AutoMapper;
using Core.Audit;
using Core.Crud;
using Core.Crud.Interfaces;
using Core.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Services
{
    public interface IProjectStatusAuditService : IPagedCrudService<ProjectStatusChangeLogDTO>
    {
    }

    public class ProjectStatusAuditService : PagedCrudService<ChangeLog, ProjectStatusChangeLogDTO>, IProjectStatusAuditService
    {
        public ProjectStatusAuditService(IAuditDataContext context, IMapper mapper) : base(context, mapper)
        {
        }

        protected override IQueryable<ChangeLog> ApplyFilter(IQueryable<ChangeLog> query, Filter filter)
        {
            var dataFilter = (StringFilter)filter.Filters.FirstOrDefault(x => x.PropertyName == "changeLogItemsText");
            if (dataFilter != null)
            {
                filter.Filters.Remove(dataFilter);
                query = query.Where(x => x.ChangeLogItems.Any(y => y.PropertyName == dataFilter.Value && y.NewValue != y.OldValue) && x.ChangeLogItems.Any(x => x.PropertyName == "Status"));
            }
            return base.ApplyFilter(query, filter);
        }

        protected override IQueryable<ChangeLog> ConfigureDataReader(IQueryable<ChangeLog> entities) => entities.Include(l => l.ChangeLogItems);
    }
}
