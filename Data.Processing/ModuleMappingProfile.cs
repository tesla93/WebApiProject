using AutoMapper;
using DataProcessing.Classes;
using DataProcessing.DTO;

namespace DataProcessing
{
    public class ModuleMappingProfile : Profile
    {
        public ModuleMappingProfile()
        {
            CreateMap<ColumnDefinitionDTO, ColumnDefinition>()
                .ForMember(d => d.TypeInfo, r => r.MapFrom(s => s.TypeInfo));
                //.ForMember(d => d.TypeInfo, r => r.Ignore());

            CreateMap<CellDataTypeInfoDTO, ICellDataTypeInfo>();

            CreateMap<ImportEntry, ImportEntryDTO>();

            CreateMap<ImportEntryCell, ImportEntryCellDTO>();

            CreateMap<DataImportResult, DataImportResultDTO>()
                .ForMember(d => d.InvalidEntries, r => r.MapFrom(s => s.InvalidEntries));

            CreateMap<DataImportConfig, DataImportConfigDTO>();
            CreateMap<DataImportConfigDTO, DataImportConfig>()
                .ForMember(d => d.ColumnDefinitions, r => r.Ignore());
        }
    }
}