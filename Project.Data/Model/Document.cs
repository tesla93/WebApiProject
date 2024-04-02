using AutoMapper;
using Core.Data;
using Project.Data.DTO;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Model
{
    [Table("Documents")]
    public class Document : IEntity
    {
        public int Id { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public string Path { get; set; }
        public int? PprojId { get; set; }


        public static void RegisterMap(IMapperConfigurationExpression c)
        {
            c.CreateMap<Document, DocumentDTO>()
                .ForMember(dest => dest.Base64, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectId, opt => opt.Ignore());

            c.CreateMap<DocumentDTO, Document>()
                .ForMember(dest => dest.PprojId, opt => opt.MapFrom(src => src.ProjectId));
        }
    }
}
