using System;
using Autofac;
using Autofac.Builder;
using Autofac.Extras.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Interceptor = AutofacExtensions.LoggingInterceptor;

namespace AutofacExtensions
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterLoggingInterceptor(this ContainerBuilder builder)
        {
            builder.RegisterType<Interceptor>().AsSelf();
        }

        public static void RegisterService<TService, TImplementation>(this ContainerBuilder builder, bool enableInterceptors = false, bool useClassInterceptors = false, ServiceLifetime? lifetime = null)
            where TService : class where TImplementation : class, TService
        {
            RegisterService(builder, typeof(TService), typeof(TImplementation), enableInterceptors, useClassInterceptors, lifetime);
        }

        public static void RegisterService<TService, TImplementation>(this ContainerBuilder builder, ServiceLifetime? lifetime)
            where TService : class where TImplementation : class, TService
        {
            RegisterService(builder, typeof(TService), typeof(TImplementation), false, false, lifetime);
        }

        public static void RegisterService<TService>(this ContainerBuilder builder, bool enableInterceptors = false, ServiceLifetime? lifetime = null) where TService : class
        {
            RegisterService(builder, typeof(TService), enableInterceptors, lifetime);
        }

        private static void RegisterService(this ContainerBuilder builder, Type serviceType, Type implementationType, bool enableInterceptors = false, bool useClassInterceptors = false, ServiceLifetime? lifetime = null)
        {
            var a = builder.RegisterType(implementationType).As(serviceType);
            ApplyLifetime(a, lifetime);


            if (!enableInterceptors) return;

            a = a.InterceptedBy(typeof(Interceptor));
            if (useClassInterceptors)
            {
                a.EnableClassInterceptors();
            }
            else
            {
                a.EnableInterfaceInterceptors();
            }
        }

        private static void RegisterService(this ContainerBuilder builder, Type serviceType, bool enableInterceptors = true, ServiceLifetime? lifetime = null)
        {
            var a = builder.RegisterType(serviceType);
            ApplyLifetime(a, lifetime);

            if (enableInterceptors)
            {
                a.EnableClassInterceptors().InterceptedBy(typeof(Interceptor));
            }
        }

        private static void ApplyLifetime(IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> builder, ServiceLifetime? lifetime)
        {
            if (lifetime == null) return;

            switch (lifetime)
            {
                case ServiceLifetime.Singleton: builder.SingleInstance(); break;
                case ServiceLifetime.Transient: builder.InstancePerDependency(); break;
                case ServiceLifetime.Scoped: builder.InstancePerRequest(); break;
            }
        }
    }
}