using System;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using Newtonsoft.Json.Serialization;

namespace Core.Web.Resolvers
{
    public class AutofacResolver : CamelCasePropertyNamesContractResolver
    {
        private readonly IContainer _container;

        public AutofacResolver(IContainer container)
        {
            _container = container;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            // Use AutoFac to create types that have been registered with it
            if (!_container.IsRegistered(objectType)) return base.CreateObjectContract(objectType);

            var contract = ResolveContact(objectType);
            contract.DefaultCreator = () => _container.Resolve(objectType);

            return contract;
        }

        private JsonObjectContract ResolveContact(Type objectType)
        {
            // Attempt to create the contact from the resolved type
            IComponentRegistration registration;
            if (!_container.ComponentRegistry.TryGetRegistration(new TypedService(objectType), out registration)) return base.CreateObjectContract(objectType);

            var viewType = (registration.Activator as ReflectionActivator)?.LimitType;
            return base.CreateObjectContract(viewType != null ? viewType : objectType);
        }
    }
}