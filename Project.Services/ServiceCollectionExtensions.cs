using Autofac;
using AutofacExtensions;
using Project.Services.Document;

namespace Project.Services
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterProjectServices(this ContainerBuilder builder)
        {
            // Register you project services, placed into Services root, here.
            // For example: builder.RegisterService<ISampleBusinessService, SampleBusinessService>();
           
            builder.RegisterService<IDocumentService, DocumentService>();
            
            builder.RegisterService<IProjectStatusAuditService, ProjectStatusAuditService>();
           

           
        }
    }
}