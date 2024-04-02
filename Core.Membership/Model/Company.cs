using Core.Data;
using System.Collections.Generic;

namespace Core.Membership.Model
{
    public class Company : IAuditableEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int Level { get; set; }


        public Address Address { get; set; }

        public int? AddressId { get; set; }

        public Branding Branding { get; set; }

        public int? BrandingId { get; set; }


        public IList<Group> Groups { get; set; }

        public string PhoneNumber { get; set; }
        public string FaxNumber { get; set; }
    }
}