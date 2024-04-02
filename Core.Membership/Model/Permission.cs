using System.Collections.Generic;
using Core.Data;

namespace Core.Membership.Model
{
    public class Permission : IAuditableEntity
    {
        public Permission() { }

        public Permission(string permissionName) => Name = permissionName;


        public int Id { get; set; }

        public string Name { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }
}
