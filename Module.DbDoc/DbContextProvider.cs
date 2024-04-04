using Module.Core.Data;
using Castle.DynamicProxy.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Module.DbDoc
{
    public class DbContextProvider : IDbContextProvider
    {
        List<Type> types = new List<Type>();
        public void Register(Type type)
        {
            if (type == null)
            {
                return;
            }

            if (!type.GetAllInterfaces().Any(i => i == typeof(IDbContext)))
            {
                throw new ApplicationException($"Type {type.Name} should implement IDbContext");
            }

            // TODO: Add logging here 

            types.Add(type);
        }

        public IDbContext[] GetDbContexts(IServiceProvider serviceProvider)
        {
            return types.Where(x=> x != null).Select(x => (IDbContext)serviceProvider.GetService(x)).ToArray();
        }
    }
}