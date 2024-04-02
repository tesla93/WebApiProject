using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.Membership.DTO;

namespace Core.Membership.Services
{
    public interface IPermissionService
    {
        Task<ICollection<PermissionDTO>> GetAll(CancellationToken cancellationToken = default);
        Task CleanupPermissions(CancellationToken cancellationToken = default);
    }
}
