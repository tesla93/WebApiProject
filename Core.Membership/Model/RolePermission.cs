using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Membership.Model
{
    public class RolePermission
    {
        public string RoleId { get; set; }

        public Role Role { get; set; }

        public int PermissionId { get; set; }

        public Permission Permission { get; set; }
    }
}
