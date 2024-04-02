using Core.Data;

namespace Core.Membership.Model
{
    public class AllowedIpUser : IEntity
    {
        public int Id { get; set; }

        public virtual AllowedIp AllowedIp { get; set; }

        public int AllowedIpId { get; set; }

        public virtual User User { get; set; }

        public string UserId { get; set; }


    }
}
