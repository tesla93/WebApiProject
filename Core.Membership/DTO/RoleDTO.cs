using Core.DTO;
using System.Collections.Generic;

namespace Core.Membership.DTO
{
    public class RoleDTO : IDTO<string>
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool AuthenticatorRequired { get; set; }
        public bool CheckIp { get; set; }

        public ICollection<PermissionDTO> Permissions { get; set; }
    }
}