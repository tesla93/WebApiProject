using AutoMapper;
using Core.Data;
using Core.Filters;
using Core.Membership.DTO;
using Core.Membership.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileStorage;
using Company = Core.Membership.Model.Company;
using Core.Exceptions;
using Core.Crud;

namespace Core.Membership.Services
{
    /// <summary>
    /// Service to provide functionality with company entities
    /// </summary>
    public class CompanyService : PagedCrudService<Company, CompanyDTO>, ICompanyService
    {
        public CompanyService(IDbContext context, IMapper mapper) : base(context, mapper)
        {
        }

        public CompanyDTO Get(string name, int skipId)
        {
            var query = GetQueryable<Company>()
                .Include(x => x.Address)
                .Include(x => x.Groups)
                .AsQueryable();

            if (skipId > 0)
            {
                query = query.Where(x => x.Id != skipId);
            }

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(x => x.Name == name);
            }

            return Mapper.Map<CompanyDTO>(query.FirstOrDefault());
        }

        protected override IQueryable<Company> ConfigureDataReader(IQueryable<Company> entities) =>
            entities.Include(x => x.Address)
            .Include(x => x.Groups)
            .Include(x => x.Branding)
            .Include(x => x.Branding.LogoImage)
            .Include(x => x.Branding.LogoIcon);

        protected override IQueryable<Company> ApplySorting(IQueryable<Company> query, ISorter command)
        {
            if (command.SortField == "PostCode")
            {
                return command.IsAsc
                    ? query.OrderBy(item => item.Address.PostCode)
                    : query.OrderByDescending(item => item.Address.PostCode);
            }

            return base.ApplySorting(query, command);
        }

        public override async Task Delete(int id, CancellationToken cancellationToken = default)
        {
            var entity = await GetQueryable<Company>().FirstOrDefaultAsync(x => x.Id == id);

            if (entity != null)
            {
                var companyHasUsers = await GetQueryable<User>().Where(x => x.CompanyId == id).AnyAsync(cancellationToken);
                if (companyHasUsers)
                {
                    throw new BusinessException("The attempted deletion has been rejected because there are users " +
                        "that depend on the company you are trying to delete.");
                }

                Remove(entity);

                await SaveChangesAsync(cancellationToken);
            }
        }
    }
}