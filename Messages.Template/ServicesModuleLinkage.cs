using Autofac;
using AutofacExtensions;
using ModuleLinkage;

namespace Messages.Templates
{
    public class ServicesModuleLinkage: IDependenciesModuleLinkage
    {
        public void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterService<IEmailTemplateParameterService, EmailTemplateParameterService>();
            builder.RegisterService<IEmailTemplateService, EmailTemplateService>();
        }
    }
}