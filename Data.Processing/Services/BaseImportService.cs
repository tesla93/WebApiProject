using AutoMapper;
using AutofacExtensions;
using DataProcessing.Classes;
using DataProcessing.DTO;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.DTO;
using Core.Crud.Interfaces;

namespace DataProcessing.Services
{
    public abstract class BaseImportService<TEntityDTO, TKey> : IImportService
        where TEntityDTO : class, IDTO<TKey>, new()
        where TKey : IEquatable<TKey>
    {
        private readonly IDataImportHelper _dataImportHelper;
        private readonly ICrudService<TEntityDTO, TKey> _entityService;

        protected readonly IHubContext<DataImportHub> HubContext;
        protected readonly IMapper Mapper;

        protected BaseImportService(
            IDataImportHelper dataImportHelper,
            ICrudService<TEntityDTO, TKey> entityService,
            IHubContext<DataImportHub> hubContext,
            IMapper mapper)
        {
            _dataImportHelper = dataImportHelper;
            _entityService = entityService;
            HubContext = hubContext;

            Mapper = mapper;
        }

        [IgnoreLogging]
        public DataImportConfig CreateSettings(ImportDataModel importDataModel, MemoryStream memoryStream, string userId)
        {
            var columnDefinitions = new ColumnsDefinitionsCollection();

            importDataModel.Config = importDataModel.Config ?? GetDefaultConfig(importDataModel);

            foreach (var colDef in importDataModel.Config.ColumnDefinitions)
            {
                var column = Mapper.Map<ColumnDefinition>(colDef);

                if (colDef.TypeInfo != null)
                {
                    InitCustomTypeInfo(column, colDef.TypeInfo);
                }
                
                try
                {
                    columnDefinitions.AddColumn(column);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException(e.Message);

                }
            }

            var dataImportConfig = new DataImportConfig();
            Mapper.Map(importDataModel.Config, dataImportConfig);
            dataImportConfig.FileName = importDataModel.FileName;
            dataImportConfig.FileStream = memoryStream;
            dataImportConfig.ColumnDefinitions = columnDefinitions;
            dataImportConfig.UserId = userId;
            dataImportConfig.UseOverride = importDataModel.UseOverride;

            return dataImportConfig;
        }

        [IgnoreLogging]
        public async Task<DataImportResultDTO> Import(DataImportConfig dataImportConfig, CancellationToken cancellationToken)
        {
            var list = new List<TEntityDTO>();
            var importResult = _dataImportHelper.ProcessDataImport(dataImportConfig,
                args =>
                {
                    if (args.Entry.IsValid)
                    {
                        var dto = CreateDTOByEntry(args.Entry);
                        if (dto != null)
                        {
                            list.Add(dto);
                        }
                    }
                });

            var res = Mapper.Map<DataImportResultDTO>(importResult);
            double percent = 0;
            int updatePercent = 0;

            if (!res.InvalidEntries.Any())
            {
                list = (await ParsedListPostprocessingAsync(list, dataImportConfig, res, cancellationToken)).ToList();

                // I think this is wrong approach. The using statement should be used for emergency disposing an external resource...
                //using (var token = _entityService.AsBulkSaveService.GetBulkToken())
                //{
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];

                    if (cancellationToken.IsCancellationRequested)
                    {
                        await HubContext.Clients.User(dataImportConfig.UserId).SendAsync("Stopped");
                        return res;
                    }

                    await _entityService.Save(item, false, cancellationToken);

                    percent = i / ((double)list.Count) * 100;

                    var intPercent = (int)percent;
                    if (intPercent > updatePercent)
                    {
                        updatePercent = intPercent;
                        await HubContext.Clients.User(dataImportConfig.UserId).SendAsync("Update", updatePercent);
                    }

                    res.ImportedCount = i + 1;
                }

                await _entityService.SaveChangesAsync(cancellationToken);
                //}
                AfterImportActions(list);
            }

            await HubContext.Clients.User(dataImportConfig.UserId).SendAsync("Result", res);

            return res;
        }

        protected virtual TEntityDTO CreateDTOByEntry(ImportEntry entry)
        {
            var dto = new TEntityDTO();
            try
            {
                foreach (var cell in entry.Cells)
                {
                    var property = typeof(TEntityDTO).GetProperties()
                        .SingleOrDefault(x => x.Name == cell.ColumnDefinition.TargetFieldName);

                    if (property != null)
                    //property.SetValue(dto, Convert.ChangeType(cell.Value, property.PropertyType));
                    {
                        var t = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                        object safeValue = (cell.Value == null) ? null : Convert.ChangeType(cell.Value, t);

                        property.SetValue(dto, safeValue, null);
                    }
                }

                return dto;
            }
            catch (Exception ex)
            {
                entry.ErrorMessage = ex.ToString();
                return null;
            }
        }

        protected virtual Task<IEnumerable<TEntityDTO>> ParsedListPostprocessingAsync(IEnumerable<TEntityDTO> list, DataImportConfig config, DataImportResultDTO result, CancellationToken cancellationToken = default) =>
            Task.Run(() => list);

        protected virtual void AfterImportActions(IEnumerable<TEntityDTO> list) { }

        public virtual DataImportConfigDTO GetDefaultConfig(ImportDataModel importDataModel) => default;

        private void InitCustomTypeInfo(ColumnDefinition columnDefinition, CellDataTypeInfoDTO typeInfo)
        {
            switch (columnDefinition.Type)
            {
                case CellDataType.Number:
                    columnDefinition.TypeInfo = new NumberCellDataTypeInfo(typeInfo.Min, typeInfo.Max);
                    break;
                case CellDataType.Date:
                case CellDataType.DateTimeOffset:
                    columnDefinition.TypeInfo = new DateTimeCellDataTypeInfo(typeInfo.DateFormats);
                    break;
                case CellDataType.Custom:
                    columnDefinition.TypeInfo = new CustomCellDataTypeInfo(GetCustomValidator(typeInfo));
                    break;
            }
        }

        protected virtual CustomValidationHandler GetCustomValidator(CellDataTypeInfoDTO typeInfo) => null;
    }

    public abstract class BaseImportService<TEntityDTO> : BaseImportService<TEntityDTO, int>, IImportService
        where TEntityDTO : class, IDTO, new()
    {
        protected BaseImportService(IDataImportHelper dataImportHelper,
            ICrudService<TEntityDTO> entityService,
            IHubContext<DataImportHub> hubContext,
            IMapper mapper) : base(dataImportHelper, entityService, hubContext, mapper)
        {
        }
    } 
}
