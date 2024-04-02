using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Crud;
using Core.Data;
using Core.Exceptions;
using Core.Filters;
using Core.Membership.DTO;
using Core.Membership.Model;
using Core.Membership.Utils;
using Core.ModelHashing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Core.Membership.Services
{
    public class RoleService : PagedCrudServiceBase<Role, RoleDTO, string>, IRoleService
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly IModelHashingService modelHashingService;

        public RoleService(
            IDbContext context,
            IMapper mapper,
            RoleManager<Role> roleManager,
            IModelHashingService modelHashingService) : base(context, mapper)
        {
            _roleManager = roleManager;
            this.modelHashingService = modelHashingService;
        }

        public override bool UseMappingOnDb { get => false; }

        protected override IQueryable<Role> GetQueryable() => _roleManager.Roles;

        protected override IQueryable<Role> ConfigureDataReader(IQueryable<Role> entities)
        {
            var coreRoles = RolesExtractor.GetAllRolesNamesOfSolution();
            return entities
                .Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
                .Where(x => !coreRoles.Contains(x.Name));
        }

        public IEnumerable<RoleDTO> GetHardcodedRoles(CancellationToken cancellationToken = default)
        {
            var coreRoles = RolesExtractor.GetAllRolesNamesOfSolution();
            return Mapper.Map<IEnumerable<RoleDTO>>(GetQueryable().Where(x => coreRoles.Contains(x.Name)));
        }

        public override async Task<RoleDTO> Save(RoleDTO dto, bool saveChanges, CancellationToken cancellationToken)
        {
            var role = Mapper.Map<Role>(dto);
            var existingRoleWithSuchName = await _roleManager.FindByNameAsync(dto.Name);
            if (dto.Id == default)
            {
                if (existingRoleWithSuchName != null)
                    throw new BusinessException("Role name already exists.");

                role.Id = Guid.NewGuid().ToString();
                await _roleManager.CreateAsync(role);
            }
            else
            {
                if (existingRoleWithSuchName != null && existingRoleWithSuchName.Id != dto.Id)
                    throw new BusinessException("Role name already exists.");

                role = await _roleManager.FindByIdAsync(dto.Id);
                if (role == null)
                    throw new ObjectNotExistsException("Role not found.");

                role.Name = dto.Name;
                await _roleManager.UpdateAsync(role);
            }

            await ReplacePermissionsForRole(role.Id, dto.Permissions ?? new List<PermissionDTO>(), cancellationToken);

            return await Get(role.Id, cancellationToken);
        }

        public override async Task Delete(string id, CancellationToken cancellationToken = default)
        {
            if (await GetQueryable<UserRole>().AnyAsync(o => o.RoleId == id, cancellationToken))
            {
                throw new BusinessException("The role is still in use. Please remove all users from this user-role before deleting it.");
            }

            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
            {
                return;
            }

            await _roleManager.DeleteAsync(role);
        }

        public async Task CleanupRoles(CancellationToken cancellationToken = default)
        {
            var exceptRoles = RolesExtractor.GetAllRolesNamesOfSolution().ToArray();
            var roles = await _roleManager.Roles.ToListAsync(cancellationToken);

            foreach (var role in roles.Where(role => exceptRoles.All(o => o != role.Name)))
            {
                await _roleManager.DeleteAsync(role);
            }
        }

        private async Task ReplacePermissionsForRole(string roleId, ICollection<PermissionDTO> newPermissionsSet, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.Roles
                .Include(x => x.RolePermissions)
                .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken);

            if (role == null) throw new ObjectNotExistsException("Role doesn't exist.");

            RemoveRange(role.RolePermissions.Where(x => newPermissionsSet.All(y => y.Id != x.PermissionId)));
            await AddRangeAsync(
                newPermissionsSet
                    .Where(x => role.RolePermissions.All(y => y.PermissionId != x.Id))
                    .Select(x => new RolePermission { RoleId = roleId, PermissionId = x.Id }),
                cancellationToken);

            await SaveChangesAsync(cancellationToken);
        }

        public override async Task<bool> Exists(string id, CancellationToken cancellationToken = default) =>
            await _roleManager.FindByIdAsync(id) != null;        

        protected override IQueryable<Role> ApplyFilter(IQueryable<Role> query, Filter filter)
        {
            if (filter.Filters.SingleOrDefault(x => x.PropertyName.ToLowerInvariant() == "permissions") is StringFilter permissionsFilter)
            {
                var unHashedPermissionId = modelHashingService.UnHashProperty<PermissionDTO>(nameof(PermissionDTO.Id), permissionsFilter.Value);
                query = query.Where(x => x.RolePermissions.Any(y => y.PermissionId == unHashedPermissionId));

                filter.Filters.Remove(permissionsFilter);
            }

            return base.ApplyFilter(query, filter);
        }
    }
}