using Core.Data;
using System.Collections.Generic;

namespace Core.Membership.Model
{   
    public class AllowedIp : IAuditableEntity
    {
        public int Id { get; set; }

        public string IpAddressFirst { get; set; }

        public string IpAddressLast { get; set; }

        public virtual ICollection<AllowedIpRole> AllowedIpRoles { get; set; }

        public virtual ICollection<AllowedIpUser> AllowedIpUsers { get; set; }
    }
}
