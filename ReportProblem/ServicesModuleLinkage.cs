using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutofacExtensions;
using System;
using ModuleLinkage;

namespace ReportProblem
{
    public class ServicesModuleLinkage: IServicesModuleLinkage, IDependenciesModuleLinkage
    {
        private const string supportSection = "SupportSettings";

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection(supportSection);
            if (section.Get<SupportSettings>() == null)
                // TODO: should be EmptyConfigurationSectionException. Resolve dependencies
                throw new Exception(supportSection);
            services.Configure<SupportSettings>(section);
        }

        public void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterService<IReportProblemService, ReportProblemService>();
        }
    }
}