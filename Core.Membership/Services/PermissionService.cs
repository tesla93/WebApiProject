using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Data;
using Core.Membership.DTO;
using Core.Membership.Model;
using Core.Membership.Utils;
using Microsoft.EntityFrameworkCore;

namespace Core.Membership.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IDbContext _context;
        private readonly IMapper _mapper;


        public PermissionService(IDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ICollection<PermissionDTO>> GetAll(CancellationToken cancellationToken = default) =>
            _mapper.Map<ICollection<PermissionDTO>>(await _context.Set<Permission>().ToListAsync(cancellationToken));

        public async Task CleanupPermissions(CancellationToken cancellationToken = default)
        {
            var exceptPermissions = PermissionsExtractor.GetAllPermissionNamesOfSolution().ToArray();
            var permissions = await _context.Set<Permission>().ToListAsync(cancellationToken);

            foreach (var permission in permissions.Where(permission => exceptPermissions.All(o => o != permission.Name)))
            {
                _context.Set<Permission>().Remove(permission);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

    }
}
