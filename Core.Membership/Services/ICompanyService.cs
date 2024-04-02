using Core.Membership.DTO;
using Core.Crud.Interfaces;

namespace Core.Membership.Services
{
    public interface ICompanyService : IPagedCrudService<CompanyDTO>
    {
        CompanyDTO Get(string name, int skipId);
    }
}