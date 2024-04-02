using Core.Data;

namespace Core.Membership.Model
{
    public class AllowedIpRole : IEntity
    {
        public int Id { get; set; }

        public virtual AllowedIp AllowedIp { get; set; }

        public int AllowedIpId { get; set; }

        public virtual Role Role { get; set; }

        public string RoleId { get; set; }
    }
}
