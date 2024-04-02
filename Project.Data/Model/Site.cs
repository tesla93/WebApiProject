using AutoMapper;
using Core.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Project.Data.DTO;

namespace Project.Data.Model
{
    [Table("Site")]

    public class Site : IAuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        //[InverseProperty(nameof(Eedfact.SiteNavigation))]
        //public virtual ICollection<Eedfact> EedfactNavigation { get; set; }

        public static void RegisterMap(IMapperConfigurationExpression c)
        {
            c.CreateMap<Site, SiteDTO>()
                .ReverseMap();
        }
    }
}