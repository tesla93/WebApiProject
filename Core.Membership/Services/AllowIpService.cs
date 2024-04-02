using AutoMapper;
using Core.Data;
using Core.Membership.DTO;
using Core.Membership.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Crud;

namespace Core.Membership.Services
{
    public class AllowIpService : PagedCrudService<AllowedIp, AllowedIpDTO>
    {
        private readonly IDbContext _context;

        public AllowIpService(IDbContext context, IMapper mapper) : base(context, mapper)
        {
            _context = context;
        }

        protected override async Task<AllowedIp> Save(AllowedIpDTO dto, AllowedIp entity, CancellationToken cancellationToken)
        {
            await UpdateAllowedIpRole(entity, dto.Roles, cancellationToken);
            await UpdateAllowedIpUser(entity, dto.Users, cancellationToken);
            return entity;
        }

        protected override IQueryable<AllowedIp> ConfigureDataReader(IQueryable<AllowedIp> entities) =>
            entities.Include(x => x.AllowedIpRoles).ThenInclude(x => x.Role)
                .Include(x => x.AllowedIpUsers).ThenInclude(x => x.User);

        
        private async Task UpdateAllowedIpRole(AllowedIp allowedIp, IList<RoleDTO> newRoles, CancellationToken cancellationToken)
        {
            if (newRoles == null) return;

            var oldRoles = _context.Set<AllowedIpRole>().Where(x => x.AllowedIpId == allowedIp.Id);
            foreach (var roleDto in newRoles)
            {
                if (oldRoles.Select(x => x.RoleId).Contains(roleDto.Id)) continue;

                var allowedIpRole = new AllowedIpRole
                {
                    RoleId = roleDto.Id,
                    AllowedIp = allowedIp
                };

                await _context.Set<AllowedIpRole>().AddAsync(allowedIpRole, cancellationToken);
            }

            foreach (var role in oldRoles)
            {
                if (newRoles.All(x => x.Id != role.RoleId))
                {
                    _context.Set<AllowedIpRole>().Remove(role);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task UpdateAllowedIpUser(AllowedIp allowedIp, IList<UserDTO> newUsers, CancellationToken cancellationToken)
        {
            if (newUsers == null) return;

            var oldUsers = _context.Set<AllowedIpUser>().Where(x => x.AllowedIpId == allowedIp.Id);
            allowedIp.AllowedIpUsers = new List<AllowedIpUser>();
            foreach (var userDto in newUsers)
            {
                if (oldUsers.Select(x => x.UserId).Contains(userDto.Id)) continue;

                var allowedIpUser = new AllowedIpUser
                {
                    UserId = userDto.Id,
                    AllowedIp = allowedIp
                };

                await _context.Set<AllowedIpUser>().AddAsync(allowedIpUser, cancellationToken);
            }

            foreach (var user in oldUsers)
            {
                if (newUsers.All(x => x.Id != user.UserId))
                {
                    _context.Set<AllowedIpUser>().Remove(user);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}