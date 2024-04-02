using System;
using Core.DTO;

namespace Project.Data.DTO
{
    public class SiteDTO : IDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

    }
}
