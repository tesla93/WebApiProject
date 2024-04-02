using Autofac;
using Microsoft.AspNetCore.Identity;
using AutofacExtensions;
using Core.Services;

namespace Core
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterCoreServices<TUser>(this ContainerBuilder builder)
            where TUser : IdentityUser
        {
            builder.RegisterService<ICurrentUserService, CurrentUserService<TUser>>();
            builder.RegisterService<IDbServices, DbServices>();
        }
    }
}