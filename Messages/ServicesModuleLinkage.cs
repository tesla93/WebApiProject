using Autofac;
using AutofacExtensions;
using Microsoft.Extensions.DependencyInjection;
using ModuleLinkage;
using Microsoft.Extensions.Configuration;
using System;

namespace Messages
{
    public class ServicesModuleLinkage : IServicesModuleLinkage, IDependenciesModuleLinkage
    {
        private const string emailSection = "EmailSettings";
        private const string smsSection = "SmsSettings";

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var sectionEmail = configuration.GetSection(emailSection);
            if (sectionEmail.Get<EmailSettings>() == null)
                // TODO: should be EmptyConfigurationSectionException. Resolve dependencies
                throw new Exception($"The configuration section '{emailSection}' is empty.");
            services.Configure<EmailSettings>(sectionEmail);

            var sectionSms = configuration.GetSection(smsSection);
            // TODO: uncomment when move to the module linkage (then handle in the module linkage's try catch)
            //if (sectionSms.Get<SMSSettings>() == null)
            //    throw new EmptyConfigurationSectionException(smsSection);
            services.Configure<SMSSettings>(sectionSms);
        }

        public void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterService<IEmailSender, MessageSender>();
            builder.RegisterService<ISmsSender, MessageSender>();
        }
    }
}