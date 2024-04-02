using System.Collections.Generic;
using Core.Membership.DTO;
using System.Threading.Tasks;
using System.Threading;
using Core.Crud.Interfaces;

namespace Core.Membership.Services
{
    public interface IRoleService : IPagedCrudService<RoleDTO, string>
    {
        IEnumerable<RoleDTO> GetHardcodedRoles(CancellationToken cancellationToken = default);
        Task CleanupRoles(CancellationToken cancellationToken = default);
    }
}
