using System;
using System.Threading.Tasks;
using Core.Crud.Interfaces;
using Core.Membership.DTO;

namespace Core.Membership.Services
{
    public interface ILoginAuditService : IPagedCrudService<LoginAuditDTO>
    {
        Task<int> GetLastAttemptsCount(string ip, DateTimeOffset withInDate);
    }
}