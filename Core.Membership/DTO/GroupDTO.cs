using Core.DTO;

namespace Core.Membership.DTO
{
    /// <summary>
    /// Data transfer object for groups
    /// </summary>
    public class GroupDTO : IDTO
    {
        /// <summary>
        /// Entity identity field
        /// </summary>
        public int Id { get; set; }
        public string Name { get; set; }

        public int? CompanyId { get; set; }
        public CompanyDTO Company { get; set; }
    }
}