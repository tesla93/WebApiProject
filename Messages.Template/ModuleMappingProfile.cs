using AutoMapper;

namespace Messages.Templates
{
    public class ModuleMappingProfile : Profile
    {
        public ModuleMappingProfile()
        {
            CreateMap<EmailTemplate, EmailTemplateDTO>().ReverseMap();
            CreateMap<EmailTemplateParameter, EmailTemplateParameterDTO>().ReverseMap();
            /*Duplicate mapping! It could be quite hard to discover if the mapping was in different file and was actually different.
            CreateMap<EmailTemplateParameter, EmailTemplateParameterDTO>();*/
        }
    }
}
