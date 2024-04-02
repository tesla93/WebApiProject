using Core.DTO;
using System.Collections.Generic;

namespace Core.Membership.DTO
{
    public class CompanyDTO : IDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Level { get; set; }
        public string PostCode => Address?.PostCode;

        public int? AddressId { get; set; }
        public AddressDTO Address { get; set; }
        public int? BrandingId { get; set; }
        public BrandingDTO Branding { get; set; }
        public IList<GroupDTO> Groups { get; set; }
        public string PhoneNumber { get; set; }
        public string FaxNumber { get; set; }
    }
}