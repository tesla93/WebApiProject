using Autofac;
using AutofacExtensions;
using Microsoft.AspNetCore.Identity;

namespace Module.Metadata
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterMetadataServices<TMetadata, TUser>(this ContainerBuilder builder)
            where TMetadata : MetadataModel<TUser>
            where TUser : IdentityUser
        {
            builder.RegisterService<IMetadataService, MetadataService<TMetadata, TUser>>();
        }
    }
}