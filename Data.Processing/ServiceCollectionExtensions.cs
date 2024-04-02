using Autofac;
using AutofacExtensions;
using DataProcessing.FileReaders;
using DataProcessing.Validation;
using DataProcessing.Services;

namespace DataProcessing
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterDataProcessingServices(this ContainerBuilder builder)
        {
            builder.RegisterService<IDataImportReaderProvider, DataImportReaderProvider>();
            builder.RegisterService<ITypeValidatorsProvider, TypeValidatorsProvider>();
            builder.RegisterService<IDataImportHelper, DataImportHelper>();
            builder.RegisterService<IGridService, GridService>();
            builder.RegisterService<IReportService, ReportService>();
            builder.RegisterService<IViewRenderService, ViewRenderService>();
        }
    }
}