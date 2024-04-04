using AutoMapper;

namespace Module.Metadata
{
    public static class ModuleMappingExtension
    {
        public static void AddMetadataMapping<TMapping>(this IMapperConfigurationExpression configuration)
            where TMapping : MetadataModelBase
        {
            configuration.CreateMap<TMapping, MetadataDTO>().ReverseMap();
        }
    }
}
