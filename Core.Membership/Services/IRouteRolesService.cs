using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Membership.DTO;

namespace Core.Membership.Services
{
    public interface IRouteRolesService
    {
        IEnumerable<ApiEndPointInfoDTO> GetRouteRoles();
        Task<IEnumerable<PageInfoDTO>> GetPagesRoles(CancellationToken cancellationToken = default);
        Task<string[]> GetPageRoutesPathsForUser(string userId, CancellationToken cancellationToken = default);
        void SetRouteRoles(PageInfoDTO[] data);
    }
}