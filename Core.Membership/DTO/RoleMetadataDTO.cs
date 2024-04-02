using System.Collections.Generic;

namespace Core.Membership.DTO
{
    public class RoleMetadataDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ICollection<string> Permissions { get; set; } = new List<string>();
    }
}