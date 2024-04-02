using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Membership.DTO
{
    public class UsersRolesReplacementDTO
    {
        public ICollection<string> UsersIds { get; set; }
        public ICollection<string> RolesIdsToAdd { get; set; }
        public ICollection<string> RolesIdsToRemove { get; set; }
    }
}
