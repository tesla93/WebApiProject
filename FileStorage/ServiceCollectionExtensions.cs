using Autofac;
using AutofacExtensions;

namespace FileStorage
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterFileStorageService(this ContainerBuilder builder)
        {
            builder.RegisterService<IFileStorageService, FileStorageService>();
        }
    }
}