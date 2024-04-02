using Core.Data;
using System.Collections.Generic;

namespace Core.Membership.Model
{
    public class Group : IAuditableEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }


        public int? CompanyId { get; set; }

        public Company Company { get; set; }


        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    }
}