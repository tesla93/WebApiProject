using System;
using System.Collections.Generic;

namespace Core.Membership.DTO
{
    // TODO: then it should become GitData/GitDataDTO<T> { LastUpdated, T Data }
    public class RolesGitDataDTO
    {
        public DateTimeOffset LastUpdated { get; set; }
        public List<RoleMetadataDTO> Roles { get; set; } = new List<RoleMetadataDTO>();
    }
}